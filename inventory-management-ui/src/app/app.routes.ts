import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { ProductListComponent } from './features/products/product-list/product-list.component';

export const routes = [
  {
    path: '',
    component: LoginComponent
  },
  {
    path: 'products',
    component: ProductListComponent
  }
];