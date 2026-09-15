import type { LoginResult } from '../types/auth'

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5106'

export async function login(
  email: string,
  password: string,
): Promise<LoginResult> {
  const response = await fetch(
    `${API_BASE_URL}/api/auth/login`,
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ email, password }),
    },
  )

  if (!response.ok) {
    if (response.status === 401) {
      throw new Error('Email o contraseña incorrectos.')
    }

    throw new Error(
      `No se pudo iniciar sesión (${response.status}).`,
    )
  }

  return response.json()
}
