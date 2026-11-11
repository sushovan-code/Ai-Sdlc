import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { API_BASE, type SetUserRoleRequest, type UserDto, type UserPage } from './api-types';

/** User administration calls (Admin role only on the server). */
@Injectable({ providedIn: 'root' })
export class UsersApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE}/users`;

  list(page: number, pageSize: number): Observable<UserPage> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http
      .get<{
        items: UserDto[];
        page: number | string;
        pageSize: number | string;
        totalCount: number | string;
      }>(this.baseUrl, { params })
      .pipe(
        map((body) => ({
          items: body.items,
          page: Number(body.page),
          pageSize: Number(body.pageSize),
          totalCount: Number(body.totalCount),
        })),
      );
  }

  setRole(userId: string, role: string): Observable<UserDto> {
    const body: SetUserRoleRequest = { role };
    return this.http.put<UserDto>(`${this.baseUrl}/${userId}/role`, body);
  }

  setActive(userId: string, isActive: boolean): Observable<UserDto> {
    const action = isActive ? 'activate' : 'deactivate';
    return this.http.post<UserDto>(`${this.baseUrl}/${userId}/${action}`, null);
  }
}
