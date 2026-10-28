import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="login-page">
      <div class="login-bg">
        <div class="bg-orb orb-1"></div>
        <div class="bg-orb orb-2"></div>
        <div class="bg-orb orb-3"></div>
      </div>
      <div class="login-container">
        <div class="login-card">
          <div class="login-header">
            <div class="login-logo">
              <span>🏋️</span>
            </div>
            <h1>GymOS</h1>
            <p>Sistema de Gestión de Gimnasios</p>
          </div>

          @if (error()) {
            <div class="alert alert-error">
              ⚠️ {{ error() }}
            </div>
          }

          <form (ngSubmit)="login()" class="login-form">
            <div class="form-group">
              <label class="form-label">Email</label>
              <input type="email" class="form-control" [(ngModel)]="email" name="email"
                placeholder="admin@gymos.com" required autocomplete="email">
            </div>
            <div class="form-group">
              <label class="form-label">Contraseña</label>
              <input [type]="showPassword() ? 'text' : 'password'" class="form-control"
                [(ngModel)]="password" name="password" placeholder="••••••••" required
                autocomplete="current-password">
            </div>
            <button type="submit" class="btn btn-primary w-full btn-lg" [disabled]="loading()">
              @if (loading()) {
                <span class="spinner-sm"></span> Ingresando...
              } @else {
                Iniciar Sesión
              }
            </button>
          </form>

          <div class="login-divider"><span>o continúa con</span></div>

          <button class="btn-google" (click)="googleLogin()" [disabled]="loading()">
            <svg width="20" height="20" viewBox="0 0 48 48">
              <path fill="#EA4335" d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z"/>
              <path fill="#4285F4" d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z"/>
              <path fill="#FBBC05" d="M10.53 28.59c-.48-1.45-.76-2.99-.76-4.59s.27-3.14.76-4.59l-7.98-6.19C.92 16.46 0 20.12 0 24c0 3.88.92 7.54 2.56 10.78l7.97-6.19z"/>
              <path fill="#34A853" d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.18 1.48-4.97 2.29-8.16 2.29-6.26 0-11.57-4.22-13.47-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48z"/>
            </svg>
            Continuar con Google
          </button>

          <div class="login-footer">
            <p>Credenciales de demo:</p>
            <code>admin&#64;gymos.com / Admin&#64;1234</code>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .login-page {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      position: relative;
      overflow: hidden;
      background: var(--bg-base);
    }
    .login-bg { position: absolute; inset: 0; pointer-events: none; }
    .bg-orb {
      position: absolute;
      border-radius: 50%;
      filter: blur(80px);
      opacity: 0.15;
      animation: float 8s ease-in-out infinite;
    }
    .orb-1 { width: 500px; height: 500px; background: var(--primary); top: -100px; left: -100px; }
    .orb-2 { width: 400px; height: 400px; background: var(--secondary); bottom: -80px; right: -80px; animation-delay: -4s; }
    .orb-3 { width: 300px; height: 300px; background: var(--accent); top: 50%; left: 50%; animation-delay: -2s; }

    @keyframes float {
      0%, 100% { transform: translate(0, 0) scale(1); }
      33% { transform: translate(30px, -30px) scale(1.05); }
      66% { transform: translate(-20px, 20px) scale(0.95); }
    }

    .login-container {
      position: relative;
      z-index: 1;
      width: 100%;
      max-width: 440px;
      padding: 1.5rem;
    }

    .login-card {
      background: rgba(26, 26, 36, 0.85);
      backdrop-filter: blur(20px);
      border: 1px solid var(--bg-border);
      border-radius: var(--radius-xl);
      padding: 2.5rem;
      box-shadow: var(--shadow-lg), 0 0 60px rgba(99,102,241,0.1);
    }

    .login-header {
      text-align: center;
      margin-bottom: 2rem;
    }

    .login-logo {
      width: 64px; height: 64px;
      background: var(--gradient-primary);
      border-radius: var(--radius-lg);
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 2rem;
      margin: 0 auto 1rem;
      box-shadow: var(--shadow-glow);
      animation: pulse-glow 3s ease-in-out infinite;
    }

    .login-header h1 {
      font-size: 2rem;
      font-weight: 900;
      background: var(--gradient-primary);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      background-clip: text;
    }

    .login-header p {
      color: var(--text-muted);
      font-size: 0.875rem;
      margin-top: 0.25rem;
    }

    .login-form { margin-bottom: 1.5rem; }

    .login-divider {
      display: flex;
      align-items: center;
      gap: 1rem;
      color: var(--text-muted);
      font-size: 0.75rem;
      margin: 1.25rem 0;
    }
    .login-divider::before, .login-divider::after {
      content: '';
      flex: 1;
      height: 1px;
      background: var(--bg-border);
    }

    .btn-google {
      width: 100%;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.75rem;
      padding: 0.75rem;
      background: rgba(255,255,255,0.05);
      border: 1px solid var(--bg-border);
      border-radius: var(--radius-md);
      color: var(--text-primary);
      font-size: 0.875rem;
      font-weight: 600;
      font-family: inherit;
      cursor: pointer;
      transition: var(--transition);
    }
    .btn-google:hover { background: rgba(255,255,255,0.1); border-color: rgba(255,255,255,0.15); }

    .login-footer {
      text-align: center;
      margin-top: 1.5rem;
      padding-top: 1.5rem;
      border-top: 1px solid var(--bg-border);
    }
    .login-footer p { font-size: 0.75rem; color: var(--text-muted); margin-bottom: 0.5rem; }
    .login-footer code {
      font-size: 0.75rem;
      background: rgba(99,102,241,0.1);
      color: var(--primary-light);
      padding: 0.25rem 0.75rem;
      border-radius: var(--radius-sm);
      border: 1px solid rgba(99,102,241,0.2);
    }

    .spinner-sm {
      display: inline-block;
      width: 16px; height: 16px;
      border: 2px solid rgba(255,255,255,0.3);
      border-top-color: white;
      border-radius: 50%;
      animation: spin 0.7s linear infinite;
    }
  `]
})
export class LoginComponent {
  email = 'admin@gymos.com';
  password = 'Admin@1234';
  loading = signal(false);
  error = signal('');
  showPassword = signal(false);

  constructor(
    private api: ApiService,
    private auth: AuthService,
    private router: Router
  ) {}

  login(): void {
    if (!this.email || !this.password) return;
    this.loading.set(true);
    this.error.set('');

    this.api.login({ email: this.email, password: this.password }).subscribe({
      next: (res) => {
        this.auth.setAuth(res);
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Error al iniciar sesión');
        this.loading.set(false);
      }
    });
  }

  googleLogin(): void {
    alert('Configura tu Google Client ID en environment.ts para habilitar el login con Google');
  }
}
