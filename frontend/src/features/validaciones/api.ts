export type Request = (path: string, init?: RequestInit) => Promise<Response>;
export async function evaluar<T>(request: Request, ruta: string, datos: unknown, signal?: AbortSignal): Promise<T> {
  const response = await request(`/api/homologaciones/${ruta}`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(datos), signal,
  });
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string; errors?: Record<string, string[]> } | null;
    throw new Error(response.status === 401 ? 'La sesión ha finalizado. Inicia sesión de nuevo.'
      : response.status === 403 ? 'No tienes permiso para realizar esta evaluación.'
      : problem?.detail || (problem?.errors && Object.values(problem.errors).flat().join(' ')) || 'No se pudo realizar la evaluación. Revisa los datos e inténtalo de nuevo.');
  }
  return response.json() as Promise<T>;
}
