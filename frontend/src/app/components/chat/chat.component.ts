// import { Component } from '@angular/core';

// @Component({
//   selector: 'app-chat',
//   templateUrl: './chat.component.html',
//   styleUrls: ['./chat.component.scss']
// })
// export class ChatComponent {

// }
// src/app/components/chat/chat.component.ts
import {
  Component,
  OnInit,
  ViewChild,
  ElementRef,
  AfterViewChecked,
} from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import {
  ChatService,
  ChatRequest,
  ModelInfo,
  Message,
} from '../../services/chat.service';

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.scss'],
})
export class ChatComponent implements OnInit, AfterViewChecked {
  @ViewChild('messagesContainer') messagesContainer!: ElementRef;

  chatForm: FormGroup;
  messages: Message[] = [];
  availableModels: ModelInfo[] = [];
  selectedModel = 'llama2';
  isLoading = false;
  isConnected = false;

  constructor(
    private fb: FormBuilder,
    private chatService: ChatService,
    private snackBar: MatSnackBar
  ) {
    this.chatForm = this.fb.group({
      message: ['', [Validators.required, Validators.minLength(1)]],
      temperature: [0.7, [Validators.min(0), Validators.max(2)]],
      maxTokens: [1000, [Validators.min(1), Validators.max(4000)]],
    });
  }

  ngOnInit(): void {
    this.checkConnection();
    this.loadAvailableModels();
  }

  ngAfterViewChecked(): void {
    this.scrollToBottom();
  }

  private scrollToBottom(): void {
    try {
      this.messagesContainer.nativeElement.scrollTop =
        this.messagesContainer.nativeElement.scrollHeight;
    } catch (err) {
      console.error('Error scrolling to bottom:', err);
    }
  }

  checkConnection(): void {
    this.chatService.checkHealth().subscribe({
      next: (response) => {
        this.isConnected = true;
        this.showSuccess('✅ Connected to backend successfully!');
      },
      error: (error) => {
        this.isConnected = false;
        this.showError('❌ Failed to connect to backend');
      },
    });
  }

  loadAvailableModels(): void {
    this.chatService.getAvailableModels().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.availableModels = response.data;
          if (
            this.availableModels.length > 0 &&
            !this.availableModels.find((m) => m.name === this.selectedModel)
          ) {
            this.selectedModel = this.availableModels[0].name;
          }
          this.showSuccess(
            `📦 Found ${this.availableModels.length} models available`
          );
        } else {
          this.showError('Failed to load models: ' + response.error);
        }
      },
      error: (error) => {
        this.showError('Failed to load available models');
      },
    });
  }

  sendMessage(): void {
    if (this.chatForm.valid && !this.isLoading) {
      const messageContent = this.chatForm.get('message')?.value.trim();
      if (!messageContent) return;

      // Add user message
      this.addMessage({
        id: this.generateId(),
        content: messageContent,
        isUser: true,
        timestamp: new Date(),
      });

      // Prepare request
      const request: ChatRequest = {
        model: this.selectedModel,
        prompt: messageContent,
        stream: false,
        options: {
          temperature: this.chatForm.get('temperature')?.value || 0.7,
          maxTokens: this.chatForm.get('maxTokens')?.value || 1000,
        },
      };

      this.isLoading = true;
      this.chatForm.get('message')?.setValue('');

      // Send to backend
      this.chatService.generateResponse(request).subscribe({
        next: (response) => {
          this.isLoading = false;

          if (response.success && response.data) {
            this.addMessage({
              id: this.generateId(),
              content: response.data.response,
              isUser: false,
              timestamp: new Date(),
              model: response.data.model,
            });
          } else {
            this.showError(response.error || 'Unknown error occurred');
          }
        },
        error: (error) => {
          this.isLoading = false;
          this.showError(error);
          this.addMessage({
            id: this.generateId(),
            content: `❌ Error: ${error}`,
            isUser: false,
            timestamp: new Date(),
            model: 'system',
          });
        },
      });
    }
  }

  pullModel(): void {
    const modelName = prompt(
      'Enter model name to download (e.g., llama2, phi3:mini):'
    );
    if (!modelName?.trim()) return;

    this.isLoading = true;
    this.showInfo(`📥 Downloading model: ${modelName}...`);

    this.chatService.pullModel(modelName).subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.success) {
          this.showSuccess(`✅ Model ${modelName} downloaded successfully!`);
          this.loadAvailableModels(); // Reload models
        } else {
          this.showError(`❌ Failed to download model: ${response.error}`);
        }
      },
      error: (error) => {
        this.isLoading = false;
        this.showError(`❌ Download failed: ${error}`);
      },
    });
  }

  clearChat(): void {
    this.messages = [];
    this.showInfo('🗑️ Chat cleared');
  }

  private addMessage(message: Message): void {
    this.messages.push(message);
  }

  private generateId(): string {
    return Date.now().toString(36) + Math.random().toString(36).substr(2);
  }

  private showSuccess(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 3000,
      panelClass: ['success-snackbar'],
    });
  }

  private showError(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      panelClass: ['error-snackbar'],
    });
  }

  private showInfo(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 3000,
      panelClass: ['info-snackbar'],
    });
  }

  onKeyPress(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }
}
