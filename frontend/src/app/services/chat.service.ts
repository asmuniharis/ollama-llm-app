// // src/app/services/chat.service.ts
// import { Injectable } from '@angular/core';
// import { HttpClient, HttpErrorResponse } from '@angular/common/http';
// import { Observable, throwError } from 'rxjs';
// import { catchError, map } from 'rxjs/operators';

// export interface ChatRequest {
//   model: string;
//   prompt: string;
//   stream: boolean;
//   options?: {
//     temperature: number;
//     maxTokens: number;
//   };
// }

// export interface ChatResponse {
//   model: string;
//   response: string;
//   done: boolean;
//   createdAt: Date;
// }

// export interface ModelInfo {
//   name: string;
//   size: string;
//   modifiedAt: Date;
// }

// export interface ApiResponse<T> {
//   success: boolean;
//   data?: T;
//   error?: string;
//   timestamp: Date;
// }

// @Injectable({
//   providedIn: 'root',
// })
// export class ChatService {
//   private readonly apiUrl = 'http://localhost:5000/api'; // Adjust based on your backend port

//   constructor(private http: HttpClient) {}

//   generateResponse(
//     request: ChatRequest
//   ): Observable<ApiResponse<ChatResponse>> {
//     return this.http
//       .post<ApiResponse<ChatResponse>>(`${this.apiUrl}/chat/generate`, request)
//       .pipe(catchError(this.handleError));
//   }

//   getAvailableModels(): Observable<ApiResponse<ModelInfo[]>> {
//     return this.http
//       .get<ApiResponse<ModelInfo[]>>(`${this.apiUrl}/chat/models`)
//       .pipe(catchError(this.handleError));
//   }

//   pullModel(modelName: string): Observable<ApiResponse<boolean>> {
//     return this.http
//       .post<ApiResponse<boolean>>(
//         `${this.apiUrl}/chat/pull-model`,
//         `"${modelName}"`,
//         {
//           headers: { 'Content-Type': 'application/json' },
//         }
//       )
//       .pipe(catchError(this.handleError));
//   }

//   checkHealth(): Observable<any> {
//     return this.http
//       .get(`${this.apiUrl}/health`)
//       .pipe(catchError(this.handleError));
//   }

//   private handleError(error: HttpErrorResponse) {
//     let errorMessage = 'An unknown error occurred!';

//     if (error.error instanceof ErrorEvent) {
//       // Client-side errors
//       errorMessage = `Error: ${error.error.message}`;
//     } else {
//       // Server-side errors
//       errorMessage = `Error Code: ${error.status}\nMessage: ${error.message}`;
//     }

//     console.error(errorMessage);
//     return throwError(() => errorMessage);
//   }
// }
// src/app/services/chat.service.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

export interface ChatRequest {
  model: string;
  prompt: string;
  stream: boolean;
  options?: {
    temperature: number;
    maxTokens: number;
  };
}

export interface ChatResponse {
  model: string;
  response: string;
  done: boolean;
  createdAt: Date;
}

export interface ModelInfo {
  name: string;
  size: string;
  modifiedAt: Date;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
  timestamp: Date;
}

export interface Message {
  id: string;
  content: string;
  isUser: boolean;
  timestamp: Date;
  model?: string;
}

@Injectable({
  providedIn: 'root',
})
export class ChatService {
  private readonly apiUrl = 'http://localhost:5114/api';

  constructor(private http: HttpClient) {}

  generateResponse(
    request: ChatRequest
  ): Observable<ApiResponse<ChatResponse>> {
    return this.http
      .post<ApiResponse<ChatResponse>>(`${this.apiUrl}/chat/generate`, request)
      .pipe(catchError(this.handleError));
  }

  getAvailableModels(): Observable<ApiResponse<ModelInfo[]>> {
    return this.http
      .get<ApiResponse<ModelInfo[]>>(`${this.apiUrl}/chat/models`)
      .pipe(catchError(this.handleError));
  }

  pullModel(modelName: string): Observable<ApiResponse<boolean>> {
    return this.http
      .post<ApiResponse<boolean>>(
        `${this.apiUrl}/chat/pull-model`,
        `"${modelName}"`,
        {
          headers: { 'Content-Type': 'application/json' },
        }
      )
      .pipe(catchError(this.handleError));
  }

  checkHealth(): Observable<any> {
    return this.http
      .get(`${this.apiUrl}/health`)
      .pipe(catchError(this.handleError));
  }

  private handleError(error: HttpErrorResponse) {
    let errorMessage = 'An unknown error occurred!';

    if (error.error instanceof ErrorEvent) {
      errorMessage = `Client Error: ${error.error.message}`;
    } else {
      errorMessage = `Server Error Code: ${error.status}\nMessage: ${error.message}`;
    }

    console.error('ChatService Error:', errorMessage);
    return throwError(() => errorMessage);
  }
}
