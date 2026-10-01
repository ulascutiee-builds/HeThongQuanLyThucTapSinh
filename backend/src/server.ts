import { app } from './app.js';
import { env } from './config/env.js';
import { pool } from './db/pool.js';
import { startEmailWorker, stopEmailWorker } from './email/worker.js';

const server = app.listen(env.PORT, () => {
  console.log(`Intern API listening on port ${env.PORT}`);
});

const stop = (signal: string) => {
  console.log(`${signal} received; shutting down`);
  void stopEmailWorker().finally(() => {
    server.close(() => {
      void pool.end().finally(() => process.exit(0));
    });
  });
};

process.once('SIGINT', () => stop('SIGINT'));
process.once('SIGTERM', () => stop('SIGTERM'));

startEmailWorker();
