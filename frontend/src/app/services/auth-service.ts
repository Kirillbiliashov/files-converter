import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { CurrentUser } from '../models/current-user';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = 'https://localhost:7099/api/auth';
  private tokenKey = 'jwtToken';
  private userKey = 'currentUser';

  constructor(private http: HttpClient) { }

  login(email: string, password: string): Observable<{ token: string }> {
    return this.http.post<{ token: string, user: CurrentUser }>(`${this.apiUrl}/login`, { email, password })
      .pipe(
        tap(response => {
          localStorage.setItem(this.tokenKey, response.token);
          localStorage.setItem(this.userKey, JSON.stringify(response.user));
        })
      );
  }


  register(username: string, email: string, password: string): Observable<{ token: string }> {
    return this.http.post<{ token: string }>(`${this.apiUrl}/register`, { username, email, password })
      .pipe(
        tap(response => {
          localStorage.setItem(this.tokenKey, response.token);
        })
      );
  }

  processOauthLogin(code: string, provider: string) {
    const body = {
      code: code,
      provider: provider
    }
    return this.http.post<{ token: string }>(`${this.apiUrl}/login/oauth/process`, body)
      .pipe(
        tap(response => {
          localStorage.setItem(this.tokenKey, response.token);
        })
      );
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }


  getCurrentUser(): CurrentUser | null {
    const userValue = localStorage.getItem(this.userKey);
    if (!userValue) return null;

    return JSON.parse(userValue);
  }

  logout() {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userKey);
  }
}
