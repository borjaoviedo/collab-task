import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { AppProviders } from './providers'
import { AppRouter } from './router'
import { hydrateAuthStoreOnBoot } from "@shared/store/auth.store"
import '@styles/globals.css'

hydrateAuthStoreOnBoot();

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AppProviders>
      <AppRouter />
    </AppProviders>
  </StrictMode>
)
