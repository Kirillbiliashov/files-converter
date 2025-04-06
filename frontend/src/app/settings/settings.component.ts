import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { UserInfo } from '../models/user-info';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.css'
})
export class SettingsComponent implements OnInit {

  userInfo!: UserInfo;

  constructor (private http: HttpClient) {}

  ngOnInit(): void {
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

}
