import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { Product } from '../../models/product';

@Injectable({
  providedIn: 'root'
})
export class ProductService {

  private readonly apiUrl =
    'http://localhost:5271/api/products';

  constructor(private http: HttpClient) {
  }

  getProducts(): Observable<Product[]> {
    return this.http.get<Product[]>(this.apiUrl);
  }

  getProductById(id: number): Observable<Product> {
    return this.http.get<Product>(
      `${this.apiUrl}/${id}`
    );
  }
}