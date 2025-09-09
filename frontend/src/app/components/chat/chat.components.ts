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
} from '../../services/chat.service';

interface Message {
  content: string;
  isUser: boolean;
  timestamp: Date;
  model?: string;
}

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
        this.snackBar.open('Connected to backend successfully!', 'Close', {
          duration: 3000,
          panelClass: ['success-snackbar'],
        });
      },
      error: (error) => {
        this.isConnected = false;
        this.snackBar.open('Failed to connect to backend', 'Close', {
          duration: 5000,
          panelClass: ['error-snackbar'],
        });
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
        }
      },
      error: (error) => {
        this.snackBar.open('Failed to load available models', 'Close', {
          duration: 5000,
          panelClass: ['error-snackbar'],
        });
      },
    });
  }

  sendMessage(): void {
    if (this.chatForm.valid && !this.isLoading) {
      const messageContent = this.chatForm.get('message')?.value.trim();
      if (!messageContent) return;

      // Add user message
      this.messages.push({
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
            this.messages.push({
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
        },
      });
    }
  }

  pullModel(modelName: string): void {
    if (!modelName.trim()) {
      this.snackBar.open('Please enter a model name', 'Close', {
        duration: 3000,
        panelClass: ['error-snackbar'],
      });
      return;
    }

    this.isLoading = true;
    this.chatService.pullModel(modelName).subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.success) {
          this.snackBar.open(
            `Model ${modelName} downloaded successfully!`,
            'Close',
            {
              duration: 5000,
              panelClass: ['success-snackbar'],
            }
          );
          this.loadAvailableModels(); // Reload models
        } else {
          this.showError(response.error || 'Failed to download model');
        }
      },
      error: (error) => {
        this.isLoading = false;
        this.showError(error);
      },
    });
  }

  clearChat(): void {
    this.messages = [];
  }

  private showError(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      panelClass: ['error-snackbar'],
    });

    // Add error message to chat
    this.messages.push({
      content: `Error: ${message}`,
      isUser: false,
      timestamp: new Date(),
      model: 'system',
    });
  }

  onKeyPress(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }
}
