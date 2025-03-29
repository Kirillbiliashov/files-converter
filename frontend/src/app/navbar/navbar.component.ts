import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, Input } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth-service';
import { CurrentUser } from '../models/current-user';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.css'
})
export class NavbarComponent {

  @Input() currentRoute!: string;
  @Input() currentUser!: CurrentUser | null;
  

  constructor(private authService: AuthService) {
  }

  logout() {
    this.authService.logout();
    this.currentUser = null;
    window.location.href = '/login';
  }
}
