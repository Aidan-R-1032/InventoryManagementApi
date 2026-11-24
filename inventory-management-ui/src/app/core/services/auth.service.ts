import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { LoginRequest } from '../../models/login-request'
import { TokenResponse } from '../../models/token-response';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private readonly apiUrl =
    'http://localhost:5271/api/auth';

  constructor(private http: HttpClient) {}

  login(dto: LoginRequest): Observable<TokenResponse> {
    return this.http.post<TokenResponse>(
      `${this.apiUrl}/login`,
      dto
    );
  }

  logout(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('role');
  }

  saveToken(response: TokenResponse): void {
    localStorage.setItem(
      'accessToken',
      response.accessToken);

    localStorage.setItem(
      'role',
      response.role);
  }

  getToken(): string | null {
    return localStorage.getItem('accessToken');
  }

  getRole(): string | null {
    return localStorage.getItem('role');
  }

  isAdmin(): boolean {
    return this.getRole() === 'Admin';
  }
}