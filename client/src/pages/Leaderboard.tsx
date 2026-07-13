import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { api } from '../services/api'

interface Entry {
  teamName: string
  totalScore: number
}

export default function Leaderboard() {
  const { gameId } = useParams<{ gameId: string }>()
  const navigate = useNavigate()
  const [entries, setEntries] = useState<Entry[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!gameId) return
    api.get(`/api/games/${gameId}/leaderboard`).then(res => {
      setEntries(res.data)
      setLoading(false)
    })
  }, [gameId])

  if (loading) return <div className="p-8 text-gray-500">Loading results...</div>

  return (
    <div className="max-w-lg mx-auto p-8">
      <h1 className="text-3xl font-bold mb-2">Final Results</h1>
      <p className="text-sm text-gray-500 mb-8">Game #{gameId}</p>

      {entries.length === 0 ? (
        <p className="text-gray-400">No scores recorded.</p>
      ) : (
        <div className="space-y-3">
          {entries.map((entry, i) => (
            <div
              key={entry.teamName}
              className={`flex items-center justify-between rounded shadow p-4 ${
                i === 0 ? 'bg-yellow-50 border border-yellow-200' : 'bg-white'
              }`}
            >
              <div className="flex items-center gap-3">
                <span className="text-lg font-bold text-gray-400 w-6">{i + 1}</span>
                <span className="font-medium">{entry.teamName}</span>
              </div>
              <span className="font-bold text-lg">{entry.totalScore} pts</span>
            </div>
          ))}
        </div>
      )}

      <button
        onClick={() => navigate('/')}
        className="mt-8 text-sm text-blue-600 hover:underline"
      >
        Back to home
      </button>
    </div>
  )
}
