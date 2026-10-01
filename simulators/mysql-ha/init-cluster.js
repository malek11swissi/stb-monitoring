// Initialisation idempotente du laboratoire MySQL InnoDB Cluster SMS.
// Ce conteneur se termine avec le code 0 une fois le cluster prêt.
const rootPassword = os.getenv('MYSQL_ROOT_PASSWORD');
const clusterPassword = os.getenv('SMS_CLUSTER_ADMIN_PASSWORD');
const appPassword = os.getenv('SMS_DATABASE_PASSWORD');
const instances = ['sms-mysql-1:3306', 'sms-mysql-2:3306', 'sms-mysql-3:3306'];

function rootUri(instance) {
  return `root:${rootPassword}@${instance}`;
}

function waitFor(instance) {
  for (let attempt = 1; attempt <= 60; attempt++) {
    try {
      const session = mysql.getSession(rootUri(instance));
      session.close();
      print(`${instance} est disponible.`);
      return;
    } catch (error) {
      print(`Attente de ${instance} (${attempt}/60)...`);
      os.sleep(2);
    }
  }
  throw new Error(`${instance} ne répond pas.`);
}

instances.forEach(waitFor);

let cluster = null;
try {
  shell.connect(`cluster_admin:${clusterPassword}@sms-mysql-1:3306`);
  cluster = dba.getCluster('smsCluster');
  print('Le cluster smsCluster existe déjà.');
} catch (error) {
  // Première exécution : configureInstance crée le compte d'administration
  // et applique les paramètres persistants requis par Group Replication.
  instances.forEach(instance => {
    dba.configureInstance(rootUri(instance), {
      clusterAdmin: 'cluster_admin',
      clusterAdminPassword: clusterPassword,
      interactive: false,
      restart: false
    });
  });
  shell.connect(`cluster_admin:${clusterPassword}@sms-mysql-1:3306`);
  cluster = dba.createCluster('smsCluster', {
    consistency: 'BEFORE_ON_PRIMARY_FAILOVER'
  });
  print('Cluster smsCluster créé.');
}

function addMember(instance) {
  const status = cluster.status({ extended: 1 });
  const topology = status.defaultReplicaSet.topology || {};
  if (Object.keys(topology).some(key => key.indexOf(instance) >= 0)) {
    print(`${instance} appartient déjà au cluster.`);
    return;
  }
  cluster.addInstance(`cluster_admin:${clusterPassword}@${instance}`, {
    recoveryMethod: 'clone'
  });
}

addMember('sms-mysql-2:3306');
addMember('sms-mysql-3:3306');

// Le schéma et le compte applicatif sont créés sur le Primary puis répliqués.
const session = mysql.getSession(rootUri('sms-mysql-1:3306'));
session.runSql('CREATE DATABASE IF NOT EXISTS sms_simulator');
session.runSql("CREATE USER IF NOT EXISTS 'sms_user'@'%' IDENTIFIED BY ?", [appPassword]);
session.runSql("GRANT ALL PRIVILEGES ON sms_simulator.* TO 'sms_user'@'%'");
session.close();

print(JSON.stringify(cluster.status({ extended: 1 }), null, 2));
print('InnoDB Cluster SMS prêt avec trois membres.');
