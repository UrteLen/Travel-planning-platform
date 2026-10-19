import { execFileSync } from 'node:child_process';
import { chmodSync, mkdirSync, readFileSync } from 'node:fs';
import { homedir } from 'node:os';
import { join } from 'node:path';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

function developmentHttps() {
  try {
    execFileSync('dotnet', ['dev-certs', 'https', '--check', '--trust'], { stdio: 'ignore' });
  } catch {
    throw new Error('A trusted .NET HTTPS certificate is required. Run: dotnet dev-certs https --trust');
  }

  const directory = join(homedir(), '.aspnet', 'https', 'travelplanner');
  const certificate = join(directory, 'localhost.pem');
  const privateKey = join(directory, 'localhost.key');
  mkdirSync(directory, { recursive: true, mode: 0o700 });
  execFileSync('dotnet', [
    'dev-certs', 'https', '--export-path', certificate, '--format', 'Pem', '--no-password'
  ], { stdio: 'inherit' });
  chmodSync(privateKey, 0o600);

  return { cert: readFileSync(certificate), key: readFileSync(privateKey) };
}

export default defineConfig(({ command, isPreview }) => ({
  plugins: [react()],
  ...(command === 'serve' && !isPreview ? {
    server: {
      host: 'localhost',
      port: 5173,
      strictPort: true,
      https: developmentHttps()
    }
  } : {})
}));
