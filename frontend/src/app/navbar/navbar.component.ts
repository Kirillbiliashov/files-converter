import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
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

  currentRoute: string = '';
  currentUser: CurrentUser | null = null;

  constructor(private router: Router, private authService: AuthService) {
    this.router.events.subscribe(() => {
      this.currentRoute = this.router.url;
      console.log(`current route: ${this.currentRoute}`);
      this.currentUser = authService.getCurrentUser();
    });
  }

  logout() {
    this.authService.logout();
    this.currentUser = null;
    window.location.href = '/login';
  }
}
