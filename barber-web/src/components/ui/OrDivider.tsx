export function OrDivider() {
  return (
    <div className="relative my-4">
      <div className="absolute inset-0 flex items-center">
        <div className="w-full border-t border-zinc-200 dark:border-zinc-700" />
      </div>
      <div className="relative flex justify-center text-sm">
        <span className="bg-white dark:bg-zinc-900 px-3 text-zinc-400 dark:text-zinc-500">
          ou
        </span>
      </div>
    </div>
  )
}