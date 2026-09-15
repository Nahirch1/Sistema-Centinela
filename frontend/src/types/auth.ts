export interface LoginResult {
  accessToken: string
  refreshToken: string
  expiresAt: string
  email: string
  displayName: string
  roles: string[]
}
