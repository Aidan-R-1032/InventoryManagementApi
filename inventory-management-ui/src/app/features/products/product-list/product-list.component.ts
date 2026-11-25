import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

import { Product } from '../../../models/product';
import { ProductService } from '../../../core/services/product.service'

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.css'
})
export class ProductListComponent
implements OnInit {

  products: Product[] = [];

  constructor(
    private productService: ProductService
  ) {
  }

  ngOnInit(): void {

    this.productService
      .getProducts()
      .subscribe({
        next: products => {
          console.log('Products:', products);
          this.products = products;
        },
        error: error => {
          console.error(error);
        }
      });
  }
}