#!/bin/sh
set -eu

# Idempotent : ne réinitialise jamais un replica set déjà présent.
until mongosh --quiet --host rne-mongo-1:27017 -u rne_user -p "$RNE_MONGO_PASSWORD" \
  --authenticationDatabase admin --eval 'db.adminCommand({ping:1}).ok' >/dev/null 2>&1; do
  sleep 2
done

mongosh --quiet --host rne-mongo-1:27017 -u rne_user -p "$RNE_MONGO_PASSWORD" \
  --authenticationDatabase admin --eval '
    try {
      rs.status();
      print("Replica set déjà configuré");
    } catch (error) {
      rs.initiate({ _id: "rs0", members: [
        { _id: 0, host: "rne-mongo-1:27017", priority: 1 },
        { _id: 1, host: "rne-mongo-2:27017", priority: 1 },
        { _id: 2, host: "rne-mongo-arbiter:27017", arbiterOnly: true }
      ] });
    }'

# Le simulateur ne démarre qu'après l'élection et la synchronisation initiale.
until mongosh --quiet --host rne-mongo-1:27017 -u rne_user -p "$RNE_MONGO_PASSWORD" \
  --authenticationDatabase admin --eval '
    const status = rs.status();
    if (!status.members.some(member => member.name === "rne-mongo-2:27017" && member.stateStr === "SECONDARY")) quit(1);' >/dev/null 2>&1; do
  sleep 2
done
