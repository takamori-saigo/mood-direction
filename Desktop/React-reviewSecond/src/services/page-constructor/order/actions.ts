import { createAsyncThunk } from '@reduxjs/toolkit';
import { orderBurgerApi } from '../../../utils/burger-api';

export const orderBurgerThunk = createAsyncThunk(
  'orders/postOrderBurger',
  async (data: string[]) => orderBurgerApi(data)
);
