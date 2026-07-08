import axios from 'axios'

export const api = axios.create()

export function setupAxiosInterceptor(getToken: () => Promise<string>) {
  api.interceptors.request.use(async config => {
    const token = await getToken()
    config.headers.Authorization = `Bearer ${token}`
    return config
  })
}
