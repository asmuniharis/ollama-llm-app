import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
})
export class AppComponent {
  title = 'Ollama LLM Frontend';

  constructor(private http: HttpClient) {}

  testBackend() {
    this.http.get('http://localhost:5114/api/health').subscribe({
      next: (response) => {
        console.log('✅ Backend response:', response);
        alert('✅ Backend connection successful!');
      },
      error: (error) => {
        console.error('❌ Backend error:', error);
        alert(
          '❌ Backend connection failed! Check if backend is running on port 5114'
        );
      },
    });
  }
}
