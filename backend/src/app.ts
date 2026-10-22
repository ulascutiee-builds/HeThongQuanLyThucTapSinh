import express from 'express';
import { pool } from './db/pool.js';
import { errorHandler } from './middleware/error-handler.js';
import { internRouter } from './routes/intern-routes.js';
import { sprint3Router } from './routes/sprint3-routes.js';

export const app = express();

app.disable('x-powered-by');
app.use(express.json({ limit: '1mb' }));

app.get('/health', async (_request, response, next) => {
  try {
    await pool.query('SELECT 1');
    response.json({ status: 'ok', database: 'connected' });
  } catch (error) {
    next(error);
  }
});

app.use('/api/interns', internRouter);
app.use('/api', sprint3Router);
app.use((_request, response) => response.status(404).json({ error: 'Không tìm thấy endpoint' }));
app.use(errorHandler);
