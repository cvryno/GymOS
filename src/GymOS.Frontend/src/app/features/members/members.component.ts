import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { Member, CreateMemberDto, PagedResult } from '../../core/models/models';

@Component({
  selector: 'app-members',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="topbar">
      <div class="topbar-title">
        <h1>Miembros</h1>
        <p>Gestión de miembros del gimnasio</p>
      </div>
      <div class="topbar-actions">
        <button class="btn btn-primary" (click)="openModal()">
          ➕ Nuevo Miembro
        </button>
      </div>
    </div>

    <div class="page-container">
      <!-- Filter Bar -->
      <div class="card mb-4">
        <div class="filter-bar">
          <div class="search-container" style="flex:1;min-width:200px">
            <span class="search-icon">🔍</span>
            <input type="text" class="form-control search-input"
              placeholder="Buscar por nombre, email, teléfono..."
              [(ngModel)]="search" (ngModelChange)="onSearch()">
          </div>
          <select class="form-control" style="width:160px" [(ngModel)]="statusFilter" (ngModelChange)="loadMembers()">
            <option value="">Todos los estados</option>
            <option value="Active">Activo</option>
            <option value="Inactive">Inactivo</option>
            <option value="Suspended">Suspendido</option>
            <option value="Expired">Expirado</option>
          </select>
          <span class="text-sm text-muted">{{ result()?.totalCount ?? 0 }} miembros</span>
        </div>
      </div>

      <!-- Table -->
      <div class="card">
        @if (loading()) {
          <div class="loading-spinner"><div class="spinner"></div></div>
        } @else if (loadError()) {
          <div class="alert alert-error">{{ loadError() }}</div>
        } @else {
          <div class="table-container">
            <table class="data-table">
              <thead>
                <tr>
                  <th>Miembro</th>
                  <th>Teléfono</th>
                  <th>Estado</th>
                  <th>Membresía Activa</th>
                  <th>Días Restantes</th>
                  <th>Registrado</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                @for (m of result()?.items; track m.id) {
                  <tr>
                    <td>
                      <div class="member-info">
                        <div class="member-avatar">{{ m.firstName.charAt(0) }}{{ m.lastName.charAt(0) }}</div>
                        <div>
                          <div class="member-name">{{ m.firstName }} {{ m.lastName }}</div>
                          <div class="member-email">{{ m.email }}</div>
                        </div>
                      </div>
                    </td>
                    <td>{{ m.phoneNumber }}</td>
                    <td>
                      <span [class]="'badge badge-' + m.status.toLowerCase()">
                        {{ m.status }}
                      </span>
                    </td>
                    <td>{{ m.activeMembership?.planName ?? '—' }}</td>
                    <td>
                      @if (m.activeMembership) {
                        <span [class]="'badge ' + (m.activeMembership.daysRemaining <= 3 ? 'badge-expired' : m.activeMembership.daysRemaining <= 7 ? 'badge-pending' : 'badge-active')">
                          {{ m.activeMembership.daysRemaining }}d
                        </span>
                      } @else {
                        <span class="text-muted">—</span>
                      }
                    </td>
                    <td class="text-muted text-sm">{{ m.createdAt | date:'dd/MM/yyyy' }}</td>
                    <td>
                      <div class="flex gap-2">
                        <button class="btn btn-sm btn-secondary" (click)="editMember(m)" title="Editar">✏️</button>
                        <button class="btn btn-sm btn-danger" (click)="deleteMember(m)" title="Eliminar">🗑️</button>
                      </div>
                    </td>
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="7">
                      <div class="empty-state">
                        <div class="empty-state-icon">👥</div>
                        <h3>No se encontraron miembros</h3>
                        <p>Agrega tu primer miembro haciendo clic en "Nuevo Miembro"</p>
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <!-- Pagination -->
          @if ((result()?.totalPages ?? 0) > 1) {
            <div class="pagination">
              <span class="text-sm text-muted">Página {{ page }} de {{ result()?.totalPages }}</span>
              <button class="page-btn" [disabled]="page <= 1" (click)="changePage(page - 1)">‹</button>
              <button class="page-btn" [disabled]="page >= (result()?.totalPages ?? 1)" (click)="changePage(page + 1)">›</button>
            </div>
          }
        }
      </div>
    </div>

    <!-- Modal -->
    @if (showModal()) {
      <div class="modal-backdrop" (click)="closeModal()">
        <div class="modal" (click)="$event.stopPropagation()">
          <div class="modal-header">
            <h3 class="modal-title">{{ editingMember() ? 'Editar Miembro' : 'Nuevo Miembro' }}</h3>
            <button class="btn btn-ghost btn-icon" (click)="closeModal()">✕</button>
          </div>
          <div class="modal-body">
            @if (formError()) {
              <div class="alert alert-error">{{ formError() }}</div>
            }
            <div class="grid-2">
              <div class="form-group">
                <label class="form-label">Nombre *</label>
                <input type="text" class="form-control" [(ngModel)]="form.firstName" placeholder="Juan">
              </div>
              <div class="form-group">
                <label class="form-label">Apellido *</label>
                <input type="text" class="form-control" [(ngModel)]="form.lastName" placeholder="Pérez">
              </div>
            </div>
            <div class="grid-2">
              <div class="form-group">
                <label class="form-label">Email *</label>
                <input type="email" class="form-control" [(ngModel)]="form.email" placeholder="juan@email.com">
              </div>
              <div class="form-group">
                <label class="form-label">Teléfono *</label>
                <input type="text" class="form-control" [(ngModel)]="form.phoneNumber" placeholder="+57 300 000 0000">
              </div>
            </div>
            <div class="grid-2">
              <div class="form-group">
                <label class="form-label">Fecha de Nacimiento</label>
                <input type="date" class="form-control" [(ngModel)]="form.dateOfBirth">
              </div>
              <div class="form-group">
                <label class="form-label">Dirección</label>
                <input type="text" class="form-control" [(ngModel)]="form.address" placeholder="Calle 123 #45-67">
              </div>
            </div>
            <div class="grid-2">
              <div class="form-group">
                <label class="form-label">Contacto de Emergencia</label>
                <input type="text" class="form-control" [(ngModel)]="form.emergencyContactName" placeholder="Nombre">
              </div>
              <div class="form-group">
                <label class="form-label">Teléfono Emergencia</label>
                <input type="text" class="form-control" [(ngModel)]="form.emergencyContactPhone" placeholder="+57 300 000 0000">
              </div>
            </div>
            <div class="form-group">
              <label class="form-label">Notas</label>
              <textarea class="form-control" rows="2" [(ngModel)]="form.notes" placeholder="Notas adicionales..."></textarea>
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-ghost" (click)="closeModal()">Cancelar</button>
            <button class="btn btn-primary" (click)="saveMember()" [disabled]="saving()">
              {{ saving() ? 'Guardando...' : (editingMember() ? 'Actualizar' : 'Crear Miembro') }}
            </button>
          </div>
        </div>
      </div>
    }
  `
})
export class MembersComponent implements OnInit {
  result = signal<PagedResult<Member> | null>(null);
  loading = signal(true);
  loadError = signal('');
  showModal = signal(false);
  editingMember = signal<Member | null>(null);
  saving = signal(false);
  formError = signal('');
  search = '';
  statusFilter = '';
  page = 1;
  searchTimeout: any;

  form: any = this.emptyForm();

  constructor(private api: ApiService) {}

  ngOnInit(): void { this.loadMembers(); }

  loadMembers(): void {
    this.loading.set(true);
    this.loadError.set('');
    this.api.getMembers(this.page, 10, this.search, this.statusFilter).subscribe({
      next: (res) => { this.result.set(res); this.loading.set(false); },
      error: (e) => {
        this.loadError.set(e.error?.message ?? 'No se pudieron cargar los miembros. Intenta nuevamente.');
        this.loading.set(false);
      }
    });
  }

  onSearch(): void {
    clearTimeout(this.searchTimeout);
    this.searchTimeout = setTimeout(() => { this.page = 1; this.loadMembers(); }, 400);
  }

  changePage(p: number): void { this.page = p; this.loadMembers(); }

  openModal(): void {
    this.editingMember.set(null);
    this.form = this.emptyForm();
    this.formError.set('');
    this.showModal.set(true);
  }

  editMember(m: Member): void {
    this.editingMember.set(m);
    this.form = {
      firstName: m.firstName, lastName: m.lastName,
      email: m.email, phoneNumber: m.phoneNumber,
      dateOfBirth: m.dateOfBirth?.substring(0, 10) ?? '',
      address: m.address, emergencyContactName: m.emergencyContactName,
      emergencyContactPhone: m.emergencyContactPhone, notes: '', status: m.status
    };
    this.formError.set('');
    this.showModal.set(true);
  }

  closeModal(): void { this.showModal.set(false); }

  saveMember(): void {
    if (!this.form.firstName || !this.form.lastName || !this.form.email) {
      this.formError.set('Nombre, apellido y email son obligatorios');
      return;
    }
    this.saving.set(true);
    const obs = this.editingMember()
      ? this.api.updateMember(this.editingMember()!.id, this.form)
      : this.api.createMember(this.form as CreateMemberDto);

    obs.subscribe({
      next: () => { this.saving.set(false); this.closeModal(); this.loadMembers(); },
      error: (e) => { this.formError.set(e.error?.message ?? 'Error al guardar'); this.saving.set(false); }
    });
  }

  deleteMember(m: Member): void {
    if (!confirm(`¿Eliminar a ${m.firstName} ${m.lastName}?`)) return;
    this.api.deleteMember(m.id).subscribe({ next: () => this.loadMembers() });
  }

  emptyForm() {
    return { firstName: '', lastName: '', email: '', phoneNumber: '',
      dateOfBirth: '', address: '', emergencyContactName: '',
      emergencyContactPhone: '', notes: '', status: 'Active' };
  }
}
