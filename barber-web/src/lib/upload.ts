// Faz upload de arquivo usando FormData
// Separado do api.ts pois usa Content-Type diferente (multipart/form-data)
export async function uploadPhoto(file: File): Promise<{ photoUrl: string }> {
  const token = localStorage.getItem('token')

  const formData = new FormData()
  formData.append('file', file)

  const response = await fetch(
    `${process.env.NEXT_PUBLIC_API_URL}/api/profile/photo`,
    {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        // Não define Content-Type — o browser define automaticamente
        // com o boundary correto para multipart/form-data
      },
      body: formData,
    }
  )

  if (!response.ok) {
    const data = await response.json()
    throw new Error(data.error ?? 'Erro ao fazer upload.')
  }

  return response.json()
}