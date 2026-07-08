import * as signalR from '@microsoft/signalr'

class SocketService {
  private connection: signalR.HubConnection | null = null

  connect() {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/game')
      .withAutomaticReconnect()
      .build()

    return this.connection.start()
  }

  disconnect() {
    this.connection?.stop()
  }

  joinRoom(gameId: string) {
    this.connection?.invoke('JoinRoom', gameId)
  }

  leaveRoom(gameId: string) {
    this.connection?.invoke('LeaveRoom', gameId)
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
