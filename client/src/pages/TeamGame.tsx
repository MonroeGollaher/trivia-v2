import { useCallback, useEffect, useRef, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useAuth0 } from '@auth0/auth0-react'
import axios from 'axios'
import { api } from '../services/api'
import { socketService } from '../services/SocketService'

interface Question {
  id: number
  text: string
  category: string
}

interface Game {
  id: number
  title: string
  roomPin: string
  activeQuestionIndex: number
}

type Stage = 'loading' | 'joining' | 'playing'

export default function TeamGame() {
  const { gameId } = useParams<{ gameId: string }>()
  const navigate = useNavigate()
  const { user } = useAuth0()
  const [stage, setStage] = useState<Stage>('loading')
  const [game, setGame] = useState<Game | null>(null)
  const [questions, setQuestions] = useState<Question[]>([])
  const [teamName, setTeamName] = useState('')
  const [answer, setAnswer] = useState('')
  const [wager, setWager] = useState(1)
  const [submitted, setSubmitted] = useState(false)
  const [result, setResult] = useState<{ approved: boolean; answer: string; wager: number; correctAnswer: string } | null>(null)
  const lastResponseIdRef = useRef<number | null>(null)

  const activeQuestion = questions[game?.activeQuestionIndex ?? 0]

  const connectSocket = useCallback((gid: string) => {
    socketService.connect().then(() => {
      socketService.joinRoom(gid)
      socketService.onNextQuestion((payload) => {
        void (async () => {
          const responseId = lastResponseIdRef.current
          lastResponseIdRef.current = null
          if (responseId) {
            try {
              const res = await api.get(`/api/responses/${responseId}/result`)
              setResult({ approved: res.data.approved, answer: res.data.answer, wager: res.data.wager, correctAnswer: res.data.correctAnswer })
            } catch {
              // result fetch failed — still advance to next question
            }
          }
          setGame(payload as Game)
          setAnswer('')
          setWager(1)
          setSubmitted(false)
        })()
      })
      socketService.onEndGame(() => {
        navigate(`/leaderboard/${gid}`)
      })
    })
  }, [navigate])

  useEffect(() => {
    if (!gameId || !user) return

    Promise.all([
      api.get(`/api/games/${gameId}`),
      api.get(`/api/questions/${gameId}`),
      api.get('/api/profiles/me').catch(() => null)
    ]).then(async ([gameRes, questionsRes, profileRes]) => {
      setGame(gameRes.data)
      setQuestions(questionsRes.data)

      if (!profileRes) {
        await api.post('/api/profiles/me', {
          email: user.email ?? '',
          name: user.name ?? '',
          picture: user.picture ?? null
        })
      }

      const profile = profileRes?.data
      if (profile?.currentGameId === gameRes.data.id && profile?.teamName) {
        setStage('playing')
        connectSocket(gameId)
      } else {
        setStage('joining')
      }
    })

    return () => {
      socketService.offNextQuestion()
      socketService.offEndGame()
      socketService.leaveRoom(gameId)
      socketService.disconnect()
    }
  }, [gameId, user, connectSocket])

  async function joinGame(e: React.FormEvent) {
    e.preventDefault()
    if (!game) return
    await api.put(`/api/profiles/joingame/${game.roomPin}`, { teamName })
    setStage('playing')
    connectSocket(gameId!)
  }

  async function submitAnswer(e: React.FormEvent) {
    e.preventDefault()
    if (!activeQuestion) return
    try {
      const res = await api.post(`/api/responses/${activeQuestion.id}`, { answer, wager })
      lastResponseIdRef.current = res.data.id
      setSubmitted(true)
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 409) {
        setSubmitted(true)
      }
    }
  }

  if (stage === 'loading') {
    return <div className="p-8 text-gray-500">Loading...</div>
  }

  if (stage === 'joining') {
    return (
      <div className="max-w-sm mx-auto p-8">
        <h1 className="text-2xl font-bold mb-6">{game?.title}</h1>
        <form onSubmit={joinGame} className="bg-white rounded shadow p-6 space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">Team Name</label>
            <input
              className="w-full border rounded px-3 py-2"
              value={teamName}
              onChange={e => setTeamName(e.target.value)}
              placeholder="Enter your team name"
              required
            />
          </div>
          <button
            type="submit"
            className="w-full bg-blue-600 text-white py-2 rounded hover:bg-blue-700"
          >
            Join Game
          </button>
        </form>
      </div>
    )
  }

  if (!activeQuestion) {
    return <div className="p-8 text-gray-500">Waiting for game to start...</div>
  }

  if (result) {
    return (
      <div className="max-w-lg mx-auto p-8 flex flex-col items-center justify-center min-h-screen">
        <div className={`w-full rounded-xl shadow-lg p-10 text-center ${result.approved ? 'bg-green-50 border border-green-200' : 'bg-red-50 border border-red-200'}`}>
          <p className={`text-5xl mb-4 ${result.approved ? 'text-green-500' : 'text-red-400'}`}>
            {result.approved ? '✓' : '✗'}
          </p>
          <h2 className={`text-2xl font-bold mb-2 ${result.approved ? 'text-green-700' : 'text-red-700'}`}>
            {result.approved ? 'Approved!' : 'Not approved'}
          </h2>
          <p className="text-gray-500 mb-1">Your answer: <span className="font-medium text-gray-700" dangerouslySetInnerHTML={{ __html: result.answer }} /></p>
          {result.approved ? (
            <p className="text-green-600 font-semibold mt-2">+{result.wager} points</p>
          ) : (
            <p className="text-gray-500 mt-2">Correct answer: <span className="font-medium text-gray-700" dangerouslySetInnerHTML={{ __html: result.correctAnswer }} /></p>
          )}
          <button
            onClick={() => setResult(null)}
            className="mt-6 px-6 py-2 bg-gray-800 text-white rounded-lg hover:bg-gray-700 text-sm"
          >
            Continue
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="max-w-lg mx-auto p-8">
      <h1 className="text-2xl font-bold mb-1">{game?.title}</h1>
      <p className="text-sm text-gray-500 mb-6">
        Question {(game?.activeQuestionIndex ?? 0) + 1}
      </p>

      <div className="bg-white rounded shadow p-6 mb-6">
        <p className="text-xs text-gray-400 uppercase mb-1">{activeQuestion.category}</p>
        <p className="text-lg font-medium"
          dangerouslySetInnerHTML={{ __html: activeQuestion.text }}
        />
      </div>

      {submitted ? (
        <div className="bg-green-50 border border-green-200 rounded p-6 text-center">
          <p className="text-green-700 font-medium">Answer submitted!</p>
          <p className="text-sm text-gray-500 mt-1">Waiting for the host to advance...</p>
        </div>
      ) : (
        <form onSubmit={submitAnswer} className="bg-white rounded shadow p-6 space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">Your Answer</label>
            <input
              className="w-full border rounded px-3 py-2"
              value={answer}
              onChange={e => setAnswer(e.target.value)}
              required
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Wager</label>
            <input
              type="number"
              min={1}
              className="w-full border rounded px-3 py-2"
              value={wager}
              onChange={e => setWager(Number(e.target.value))}
            />
          </div>
          <button
            type="submit"
            className="w-full bg-blue-600 text-white py-2 rounded hover:bg-blue-700"
          >
            Submit Answer
          </button>
        </form>
      )}
    </div>
  )
}
