import { createAsyncThunk } from '@reduxjs/toolkit';
import { getFeedsApi, getOrderByNumberApi } from '../../../utils/burger-api';

export const getFeedsThunk = createAsyncThunk('feeds/getFeeds', async () =>
  getFeedsApi()
);

export const getOrderByNumberThunk = createAsyncThunk(
  'orders/getOrder',
  async (number: number) => getOrderByNumberApi(number)
);
