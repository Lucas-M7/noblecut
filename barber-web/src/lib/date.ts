export function getLocalToday(): string {
    const now = new Date()
    const year = now.getFullYear()
    const month = String(now.getMonth() + 1).padStart(2, '0')
    const day = String(now.getDate()).padStart(2, '0')
    return `${year}-${month}-${day}`
}

export function formatDate(d: string): string {
    const [y, m, day] = d.split('-')
    return `${day}/${m}/${y}`
}