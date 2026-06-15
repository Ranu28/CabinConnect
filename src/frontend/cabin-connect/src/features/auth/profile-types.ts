export type UserRole = 'guest' | 'host' | 'admin'

export interface UserProfile {
  id: string
  role: UserRole
  displayName: string
}
