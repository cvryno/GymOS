import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { Membership, MembershipPlan, Member } from '../../core/models/models';

@Component({ selector: 'app-memberships', standalone: true, imports: [CommonModule, FormsModule], template: `
<div class="topbar"><div class="topbar-title"><h1>Membresías</h1><p>Planes, renovaciones y vencimientos</p></div></div><div class="page-container">
<div class="card mb-4"><h3>Crear o renovar membresía</h3><div class="filter-bar"><select class="form-control" [(ngModel)]="memberId"><option [ngValue]="0">Selecciona miembro</option>@for(m of members();track m.id){<option [ngValue]="m.id">{{m.firstName}} {{m.lastName}}</option>}</select><select class="form-control" [(ngModel)]="planId"><option [ngValue]="0">Selecciona plan</option>@for(p of plans();track p.id){<option [ngValue]="p.id">{{p.name}} · {{p.price | currency}}</option>}</select><input class="form-control" type="date" [(ngModel)]="startDate"><button class="btn btn-primary" (click)="create()">Registrar</button></div></div>
<div class="card"><h3>Próximos vencimientos</h3>@if(error()){<div class="alert alert-error">{{error()}}</div>}<div class="table-container"><table class="data-table"><thead><tr><th>Miembro</th><th>Plan</th><th>Estado</th><th>Vence</th><th>Restan</th><th>Acción</th></tr></thead><tbody>@for(m of memberships();track m.id){<tr><td>{{m.memberName}}</td><td>{{m.planName}}</td><td>{{m.status}}</td><td>{{m.endDate|date:'dd/MM/yyyy'}}</td><td>{{m.daysRemaining}} días</td><td><button class="btn btn-sm btn-secondary" (click)="notify(m.id)">WhatsApp</button></td></tr>}@empty{<tr><td colspan="6">No hay membresías próximas a vencer.</td></tr>}</tbody></table></div></div></div>` })
export class MembershipsComponent implements OnInit {
 memberships=signal<Membership[]>([]); plans=signal<MembershipPlan[]>([]); members=signal<Member[]>([]); error=signal(''); memberId=0; planId=0; startDate=new Date().toISOString().slice(0,10);
 constructor(private api:ApiService){} ngOnInit(){this.api.getMemberships(1,100).subscribe(x=>this.memberships.set(x.items));this.api.getPlans().subscribe(x=>this.plans.set(x.filter(p=>p.isActive)));this.api.getMembers(1,100).subscribe(x=>this.members.set(x.items));}
 create(){const plan=this.plans().find(p=>p.id===this.planId);if(!this.memberId||!plan){this.error.set('Selecciona miembro y plan.');return;}this.api.createMembership({memberId:this.memberId,planId:this.planId,startDate:this.startDate,amountPaid:plan.price}).subscribe({next:()=>{this.error.set('Membresía registrada.');this.ngOnInit();},error:e=>this.error.set(e.error?.message??'No se pudo registrar la membresía.')});}
 notify(id:number){this.api.sendWhatsAppNotification(id).subscribe({next:()=>this.error.set('Notificación enviada.'),error:e=>this.error.set(e.error?.message??'No se pudo enviar la notificación.')});}
}
