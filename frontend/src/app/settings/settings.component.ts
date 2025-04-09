import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit } from '@angular/core';
import { UserInfo } from '../models/user-info';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AuthService } from '../services/auth-service';
import { FormsModule } from '@angular/forms';
import { UserService } from '../services/http/user-service';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.css'
})
export class SettingsComponent implements OnInit {
  private route = inject(ActivatedRoute);
  userInfo!: UserInfo;
  previousUrl: string | null = null;
  private debounceTimer: any;

  constructor (
    private userService: UserService,
    private authService: AuthService) {}

  ngOnInit(): void {
    this.loadUserInfo();
    this.route.queryParams.subscribe(params => {
      this.previousUrl = params['from'];
    });
  }

  private loadUserInfo() {
    this.userService.getUserInfo()
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
    this.userService.deleteUserAccount()
    .subscribe({
      next: (response) => {
        this.authService.logout();
      },
      error: (error) => {
        console.log(`error, ${error}`)
      }
    });
  }

  onSwitchChanged() {
    if (this.debounceTimer) {
      clearTimeout(this.debounceTimer);
    }
 
    this.debounceTimer = setTimeout(() => {
      this.changePreferences();
    }, 500);
  }

  changePreferences() {
    this.userService.updatePreferences(this.userInfo.deleteFilesAutomatically)
    .subscribe();
  }

}
