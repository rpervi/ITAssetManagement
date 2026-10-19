export interface AuthenticatedUser {
  userId: number;
  username: string;
  email: string;
  roles: string[];
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
}

export interface ApiMessage {
  message: string;
}
