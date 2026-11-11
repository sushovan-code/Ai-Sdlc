import type { components } from './schema';

/** API contract types, generated from the backend OpenAPI document (`npm run api:types`). */
export type UserDto = components['schemas']['UserDto'];
export type AuthTokenDto = components['schemas']['AuthTokenDto'];
export type RegisterRequest = components['schemas']['RegisterRequest'];
export type LoginRequest = components['schemas']['LoginRequest'];
export type ChangePasswordRequest = components['schemas']['ChangePasswordRequest'];
export type SetUserRoleRequest = components['schemas']['SetUserRoleRequest'];

/** A page of users. The OpenAPI document types integers as `number | string`; the client normalises them. */
export interface UserPage {
  items: UserDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export const API_BASE = '/api/v1';
