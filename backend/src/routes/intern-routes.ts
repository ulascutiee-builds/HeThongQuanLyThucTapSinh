import { Router } from 'express';
import { createInternSchema, listInternQuerySchema } from '../validation/intern.js';
import { createIntern, listInterns } from '../services/intern-service.js';
import { asyncHandler } from '../utils/async-handler.js';

export const internRouter = Router();

internRouter.post('/', asyncHandler(async (request, response) => {
  const input = createInternSchema.parse(request.body);
  const intern = await createIntern(input);
  response.status(201).json({ data: intern });
}));

internRouter.get('/', asyncHandler(async (request, response) => {
  const filters = listInternQuerySchema.parse(request.query);
  const result = await listInterns(filters);
  response.json(result);
}));
