import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideCompass,
  lucideGlobe2,
  lucidePlane,
} from '@ng-icons/lucide';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, NgIcon],
  providers: [
    provideIcons({
      lucideCompass,
      lucideGlobe2,
      lucidePlane,
    }),
  ],
  templateUrl: './app.html',
})
export class App {}
