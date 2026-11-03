export type Role = 'Administrador' | 'Docente' | 'Coordinador';
export interface Credentials { nombreUsuario: string; password: string }
export interface LoginResponse { accessToken: string; tokenType: string; expiresAtUtc: string; name: string; role: Role }
export interface Session { accessToken: string; name: string; role: Role; expiresAt: number }
