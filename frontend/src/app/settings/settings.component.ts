import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit } from '@angular/core';
import { UserInfo } from '../models/user-info';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AuthService } from '../services/auth-service';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.css'
})
export class SettingsComponent implements OnInit {
  private route = inject(ActivatedRoute);
  userInfo!: UserInfo;
  previousUrl: string | null = null;

  constructor (
    private http: HttpClient,
    private authService: AuthService) {}

  ngOnInit(): void {
    this.loadUserInfo();
    this.route.queryParams.subscribe(params => {
      this.previousUrl = params['from'];
    });
  }

  private loadUserInfo() {
    this.http.get<UserInfo>(`https://localhost:7099/api/user`)
    .subscribe({
      next: (userInfo) => {
        this.userInfo = userInfo;
      },
      error: (error) => {
        console.log(`error, ${error}`)
      }
    });
  }

  deleteAccount() {
    this.http.post(`https://localhost:7099/api/user/delete`, {})
    .subscribe({
      next: (response) => {
        this.authService.logout();
      },
      error: (error) => {
        console.log(`error, ${error}`)
      }
    });
  }

}
