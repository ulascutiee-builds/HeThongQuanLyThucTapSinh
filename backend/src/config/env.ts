import 'dotenv/config';
import { z } from 'zod';

const envSchema = z.object({
  NODE_ENV: z.enum(['development', 'test', 'production']).default('development'),
  PORT: z.coerce.number().int().positive().default(3000),
  DATABASE_URL: z.string().min(1, 'DATABASE_URL is required'),
  EMAIL_WORKER_ENABLED: z.string().default('false').transform((value) => value.toLowerCase() === 'true'),
  SMTP_HOST: z.string().default('localhost'),
  SMTP_PORT: z.coerce.number().int().positive().default(587),
  SMTP_SECURE: z.string().default('false').transform((value) => value.toLowerCase() === 'true'),
  SMTP_USER: z.string().default(''),
  SMTP_PASSWORD: z.string().default(''),
  EMAIL_FROM: z.string().default('Intern Management <no-reply@example.com>'),
  EMAIL_WORKER_INTERVAL_MS: z.coerce.number().int().positive().default(5000),
  EMAIL_MAX_ATTEMPTS: z.coerce.number().int().positive().default(5),
});

export const env = envSchema.parse(process.env);

if (env.EMAIL_WORKER_ENABLED && (!env.SMTP_HOST || !env.EMAIL_FROM)) {
  throw new Error('SMTP_HOST and EMAIL_FROM are required when EMAIL_WORKER_ENABLED=true');
}
