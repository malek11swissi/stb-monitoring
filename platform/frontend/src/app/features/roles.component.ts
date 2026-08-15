import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../core/auth.service';
interface Role {
  id: string;
  name: string;
  description: string;
  isActive: boolean;
  permissions: string[];
}
interface Permission {
  id: string;
  name: string;
  description: string;
}
@Component({
  standalone: true,
  imports: [FormsModule, CommonModule],
  template: `<section class="page rbac">
    <div class="rbac-hero">
      <div>
        <span class="eyebrow">CONTRÔLE DES ACCÈS</span>
        <h1>Rôles & permissions</h1>
        <p>Configurez précisément qui peut accéder à chaque fonction.</p>
      </div>
      <div class="summary">
        <strong>{{ roles.length }}</strong
        ><span>rôles</span><strong>{{ permissions.length }}</strong
        ><span>permissions</span>
      </div>
    </div>
    @if (message) {
      <div class="success">{{ message }}</div>
    }
    @if (error) {
      <div class="error">{{ error }}</div>
    }
    @if (auth.hasPermission('roles.manage')) {
      <div class="creation-grid">
        <form
          #roleForm="ngForm"
          class="card compact-form"
          (ngSubmit)="createRole()"
        >
          <div class="form-icon role-icon">◇</div>
          <div>
            <h2>Nouveau rôle</h2>
            <p>Créez un profil d’accès.</p>
          </div>
          <label
            >Nom<input
              #roleName="ngModel"
              name="name"
              [(ngModel)]="roleDraft.name"
              placeholder="MANAGER_SECURITY"
              required
              minlength="3"
              pattern="^[A-Za-z][A-Za-z0-9_]*$" /></label
          ><label
            >Description<input
              name="description"
              [(ngModel)]="roleDraft.description"
              placeholder="Responsable sécurité"
          /></label>
          @if (roleName.touched && roleName.invalid) {
            <small class="field-error"
              >Lettres, chiffres et underscore uniquement.</small
            >
          }
          <button class="primary" [disabled]="roleForm.invalid">
            Créer le rôle
          </button>
        </form>
        <form
          #permissionForm="ngForm"
          class="card compact-form"
          (ngSubmit)="createPermission()"
        >
          <div class="form-icon permission-icon">✓</div>
          <div>
            <h2>Nouvelle permission</h2>
            <p>Ajoutez une capacité technique.</p>
          </div>
          <label
            >Nom technique<input
              #permissionName="ngModel"
              name="permissionName"
              [(ngModel)]="permissionDraft.name"
              placeholder="security.read"
              required
              minlength="3"
              pattern="^[a-z][a-z0-9_.]*$" /></label
          ><label
            >Description<input
              name="permissionDescription"
              [(ngModel)]="permissionDraft.description"
              placeholder="Consulter la sécurité"
          /></label>
          @if (permissionName.touched && permissionName.invalid) {
            <small class="field-error">Format attendu : resource.action</small>
          }
          <button class="primary" [disabled]="permissionForm.invalid">
            Créer la permission
          </button>
        </form>
      </div>
    }
    <section class="card catalog">
      <div class="section-title">
        <div>
          <span class="eyebrow">CATALOGUE</span>
          <h2>Permissions disponibles</h2>
        </div>
        <span class="count">{{ permissions.length }}</span>
      </div>
      <div class="permission-list">
        @for (permission of permissions; track permission.id) {
          <div class="permission-row">
            <span class="permission-dot"></span>
            <div>
              <strong>{{ permission.name }}</strong
              ><small>{{
                permission.description || 'Aucune description'
              }}</small>
            </div>
            <span class="usage">{{ usage(permission.name) }} rôle(s)</span>
            @if (auth.hasPermission('roles.manage')) {
              <button
                class="icon-button"
                title="Modifier"
                (click)="editPermission(permission)"
              >
                ✎</button
              ><button
                class="icon-button danger"
                title="Supprimer"
                (click)="deletePermission(permission)"
              >
                ×
              </button>
            }
          </div>
        }
      </div>
    </section>
    <div class="section-title role-heading">
      <div>
        <span class="eyebrow">MATRICE RBAC</span>
        <h2>Configuration des rôles</h2>
      </div>
    </div>
    <div class="role-grid">
      @for (role of roles; track role.id) {
        <article class="card role-card" [class.archived]="!role.isActive">
          <div class="role-top">
            <div class="role-avatar">{{ role.name.charAt(0) }}</div>
            <div>
              <h2>{{ role.name }}</h2>
              <span
                [class]="role.isActive ? 'status active' : 'status inactive'"
                >{{ role.isActive ? 'Actif' : 'Archivé' }}</span
              >
            </div>
            <button
              class="icon-button"
              (click)="editRole(role)"
              *ngIf="auth.hasPermission('roles.manage')"
            >
              ✎
            </button>
          </div>
          <p>{{ role.description || 'Aucune description' }}</p>
          <div class="permission-checks">
            @for (permission of permissions; track permission.id) {
              <label
                ><input
                  type="checkbox"
                  [checked]="role.permissions.includes(permission.name)"
                  (change)="
                    toggle(role, permission.name, $any($event.target).checked)
                  "
                  [disabled]="
                    !auth.hasPermission('roles.manage') || !role.isActive
                  "
                /><span
                  ><strong>{{ permission.name }}</strong
                  ><small>{{ permission.description }}</small></span
                ></label
              >
            }
          </div>
          @if (auth.hasPermission('roles.manage')) {
            <button class="archive-button" (click)="active(role)">
              {{ role.isActive ? 'Archiver le rôle' : 'Réactiver le rôle' }}
            </button>
            @if (role.name !== 'ADMIN') {
              <button class="delete-role" (click)="deleteRole(role)">
                Supprimer définitivement
              </button>
            }
          }
        </article>
      }
    </div>
    @if (selectedRole) {
      <div class="modal-backdrop" (click)="selectedRole = null">
        <form
          #editRoleForm="ngForm"
          class="card modal form"
          (click)="$event.stopPropagation()"
          (ngSubmit)="saveRole()"
        >
          <div class="page-title">
            <h2>Modifier le rôle</h2>
            <button type="button" (click)="selectedRole = null">×</button>
          </div>
          <label
            >Nom<input
              name="editRoleName"
              [(ngModel)]="roleEdit.name"
              required
              pattern="^[A-Za-z][A-Za-z0-9_]*$" /></label
          ><label
            >Description<input
              name="editRoleDescription"
              [(ngModel)]="roleEdit.description" /></label
          ><button class="primary" [disabled]="editRoleForm.invalid">
            Enregistrer
          </button>
        </form>
      </div>
    }
    @if (selectedPermission) {
      <div class="modal-backdrop" (click)="selectedPermission = null">
        <form
          #editPermissionForm="ngForm"
          class="card modal form"
          (click)="$event.stopPropagation()"
          (ngSubmit)="savePermission()"
        >
          <div class="page-title">
            <h2>Modifier la permission</h2>
            <button type="button" (click)="selectedPermission = null">×</button>
          </div>
          <label
            >Nom technique<input
              name="editPermissionName"
              [(ngModel)]="permissionEdit.name"
              required
              pattern="^[a-z][a-z0-9_.]*$" /></label
          ><label
            >Description<input
              name="editPermissionDescription"
              [(ngModel)]="permissionEdit.description" /></label
          ><button class="primary" [disabled]="editPermissionForm.invalid">
            Enregistrer
          </button>
        </form>
      </div>
    }
  </section>`,
  styles: [
    `
      .rbac {
        max-width: 1280px;
      }
      .rbac-hero {
        display: flex;
        justify-content: space-between;
        align-items: end;
        margin-bottom: 25px;
      }
      .eyebrow {
        font-size: 10px;
        font-weight: 800;
        letter-spacing: 0.14em;
        color: #078578;
      }
      .rbac-hero h1 {
        font-size: 32px;
        margin: 6px 0;
      }
      .rbac-hero p,
      .compact-form p {
        color: #738495;
        margin: 0;
      }
      .summary {
        display: grid;
        grid-template-columns: auto auto;
        column-gap: 8px;
        background: #0e2237;
        color: white;
        padding: 13px 18px;
        border-radius: 14px;
      }
      .summary strong {
        font-size: 20px;
      }
      .summary span {
        font-size: 11px;
        color: #8fa2b5;
        align-self: center;
      }
      .creation-grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 18px;
        margin-bottom: 20px;
      }
      .compact-form {
        display: grid;
        grid-template-columns: auto 1fr;
        gap: 12px 15px;
      }
      .compact-form label,
      .compact-form button,
      .compact-form > .field-error {
        grid-column: 1/-1;
      }
      .compact-form h2 {
        margin: 0;
        font-size: 18px;
      }
      .form-icon {
        width: 43px;
        height: 43px;
        display: grid;
        place-items: center;
        border-radius: 12px;
        font-size: 22px;
      }
      .role-icon {
        background: #e5f7f4;
        color: #078578;
      }
      .permission-icon {
        background: #eeeaff;
        color: #7654c4;
      }
      .catalog {
        margin-bottom: 26px;
      }
      .section-title {
        display: flex;
        justify-content: space-between;
        align-items: center;
      }
      .section-title h2 {
        margin: 4px 0 14px;
      }
      .count {
        background: #e8f5f3;
        color: #087e76;
        border-radius: 999px;
        padding: 6px 11px;
        font-weight: 800;
      }
      .permission-list {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 0 24px;
      }
      .permission-row {
        display: grid;
        grid-template-columns: auto 1fr auto auto auto;
        gap: 10px;
        align-items: center;
        padding: 13px 4px;
        border-top: 1px solid #e8eef2;
      }
      .permission-row div {
        display: grid;
      }
      .permission-row small {
        color: #8191a0;
      }
      .permission-dot {
        width: 9px;
        height: 9px;
        border-radius: 50%;
        background: #12a594;
      }
      .usage {
        font-size: 10px;
        color: #708292;
        background: #f0f4f7;
        padding: 4px 7px;
        border-radius: 999px;
      }
      .icon-button {
        width: 31px;
        height: 31px;
        border: 1px solid #dce5eb;
        border-radius: 8px;
        background: white;
        color: #536b7d;
      }
      .icon-button.danger {
        color: #c33930;
      }
      .role-heading {
        margin-top: 15px;
      }
      .role-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
        gap: 18px;
      }
      .role-card {
        padding: 20px;
      }
      .role-top {
        display: grid;
        grid-template-columns: auto 1fr auto;
        gap: 12px;
        align-items: center;
      }
      .role-avatar {
        width: 45px;
        height: 45px;
        border-radius: 13px;
        display: grid;
        place-items: center;
        background: linear-gradient(135deg, #0d8178, #16a6a0);
        color: white;
        font-size: 20px;
        font-weight: 900;
      }
      .role-top h2 {
        margin: 0;
        font-size: 18px;
      }
      .role-card > p {
        color: #718291;
        min-height: 38px;
      }
      .permission-checks {
        max-height: 285px;
        overflow: auto;
        border: 1px solid #e3eaf0;
        border-radius: 11px;
      }
      .permission-checks label {
        display: grid;
        grid-template-columns: auto 1fr;
        align-items: center;
        gap: 10px;
        padding: 10px 12px;
        border-bottom: 1px solid #edf1f4;
      }
      .permission-checks label:last-child {
        border: 0;
      }
      .permission-checks span {
        display: grid;
      }
      .permission-checks small {
        color: #8594a2;
      }
      .archive-button {
        margin-top: 14px;
        width: 100%;
        border: 1px solid #e2b5b2;
        background: #fff;
        color: #ae362f;
        border-radius: 9px;
        padding: 9px;
      }
      .delete-role {
        margin-top: 8px;
        width: 100%;
        border: 0;
        background: transparent;
        color: #b42318;
        padding: 8px;
        font-weight: 700;
      }
      @media (max-width: 900px) {
        .creation-grid,
        .permission-list {
          grid-template-columns: 1fr;
        }
      }
      @media (max-width: 600px) {
        .rbac-hero {
          align-items: start;
          flex-direction: column;
          gap: 15px;
        }
        .role-grid {
          grid-template-columns: 1fr;
        }
        .permission-row {
          grid-template-columns: auto 1fr auto;
        }
        .usage {
          display: none;
        }
      }
    `,
  ],
})
export class RolesComponent implements OnInit {
  roles: Role[] = [];
  permissions: Permission[] = [];
  selectedRole: Role | null = null;
  selectedPermission: Permission | null = null;
  message = '';
  error = '';
  roleDraft = { name: '', description: '' };
  permissionDraft = { name: '', description: '' };
  roleEdit = { name: '', description: '' };
  permissionEdit = { name: '', description: '' };
  readonly api = 'http://localhost:5041/api';
  constructor(
    private http: HttpClient,
    public auth: AuthService,
  ) {}
  ngOnInit() {
    this.loadAll();
  }
  loadAll() {
    this.http
      .get<Role[]>(`${this.api}/roles`)
      .subscribe(
        (x) =>
          (this.roles = x.map((r) => ({
            ...r,
            isActive: r.isActive !== false,
          }))),
      );
    this.http
      .get<Permission[]>(`${this.api}/permissions`)
      .subscribe((x) => (this.permissions = x));
  }
  usage(name: string) {
    return this.roles.filter((r) => r.permissions.includes(name)).length;
  }
  createRole() {
    this.request(
      this.http.post(`${this.api}/roles`, this.roleDraft),
      'Rôle créé.',
      () => (this.roleDraft = { name: '', description: '' }),
    );
  }
  createPermission() {
    this.request(
      this.http.post(`${this.api}/permissions`, this.permissionDraft),
      'Permission créée.',
      () => (this.permissionDraft = { name: '', description: '' }),
    );
  }
  editRole(r: Role) {
    this.selectedRole = r;
    this.roleEdit = { name: r.name, description: r.description };
  }
  saveRole() {
    if (!this.selectedRole) return;
    this.request(
      this.http.put(`${this.api}/roles/${this.selectedRole.id}`, this.roleEdit),
      'Rôle modifié.',
      () => (this.selectedRole = null),
    );
  }
  editPermission(p: Permission) {
    this.selectedPermission = p;
    this.permissionEdit = { name: p.name, description: p.description };
  }
  savePermission() {
    if (!this.selectedPermission) return;
    this.request(
      this.http.put(
        `${this.api}/permissions/${this.selectedPermission.id}`,
        this.permissionEdit,
      ),
      'Permission modifiée.',
      () => (this.selectedPermission = null),
    );
  }
  deletePermission(p: Permission) {
    if (!confirm(`Supprimer la permission ${p.name} ?`)) return;
    this.request(
      this.http.delete(`${this.api}/permissions/${p.id}`),
      'Permission supprimée.',
    );
  }
  deleteRole(r: Role) {
    if (!confirm(`Supprimer définitivement le rôle ${r.name} ?`)) return;
    this.request(
      this.http.delete(`${this.api}/roles/${r.id}`),
      'Rôle supprimé.',
    );
  }
  active(r: Role) {
    this.request(
      this.http.patch(
        `${this.api}/roles/${r.id}/active?value=${!r.isActive}`,
        {},
      ),
      `Rôle ${r.isActive ? 'archivé' : 'réactivé'}.`,
    );
  }
  toggle(r: Role, p: string, on: boolean) {
    const permissions = on
      ? [...r.permissions, p]
      : r.permissions.filter((x) => x !== p);
    this.request(
      this.http.put(`${this.api}/roles/${r.id}/permissions`, { permissions }),
      'Permissions mises à jour.',
    );
  }
  request(call: any, success: string, after?: () => void) {
    this.clear();
    call.subscribe({
      next: () => {
        after?.();
        this.message = success;
        this.loadAll();
      },
      error: (e: any) => this.fail(e),
    });
  }
  clear() {
    this.message = '';
    this.error = '';
  }
  fail(e: any) {
    const validation = Object.values(e.error?.errors || {})
      .flat()
      .join(' ');
    this.error =
      e.error?.message ||
      validation ||
      (e.status === 403
        ? 'Permission roles.manage requise.'
        : `Erreur HTTP ${e.status || 0}.`);
  }
}
