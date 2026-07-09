import * as signalR from '@microsoft/signalr'

class SocketService {
  private connection: signalR.HubConnection | null = null

  async connect() {
    if (this.connection?.state === signalR.HubConnectionState.Connected) return

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/game')
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    try {
      await this.connection.start()
      console.log('[SignalR] Connected')
    } catch (err) {
      console.error('[SignalR] Connection failed:', err)
    }
  }

  disconnect() {
    this.connection?.stop()
    this.connection = null
  }

  joinRoom(gameId: string) {
    this.connection?.invoke('JoinRoom', gameId)
  }

  leaveRoom(gameId: string) {
    this.connection?.invoke('LeaveRoom', gameId)
  }

  nextQuestion(gameId: string, payload: unknown) {
    this.connection?.invoke('NextQuestion', gameId, payload)
  }

  onNextQuestion(callback: (payload: unknown) => void) {
    this.connection?.on('nextQuestion', callback)
  }

  onEndGame(callback: (payload: unknown) => void) {
    this.connection?.on('endGame', callback)
  }

  onOrderRanking(callback: (payload: unknown) => void) {
    this.connection?.on('orderRanking', callback)
  }

  offNextQuestion() {
    this.connection?.off('nextQuestion')
  }

  offEndGame() {
    this.connection?.off('endGame')
  }

  offOrderRanking() {
    this.connection?.off('orderRanking')
  }
}

export const socketService = new SocketService()
