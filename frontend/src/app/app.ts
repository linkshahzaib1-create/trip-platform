import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucidePlane } from '@ng-icons/lucide';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, NgIcon],
  providers: [provideIcons({ lucidePlane })],
  templateUrl: './app.html',
})
export class App {
  private readonly router = inject(Router);

  planTrip(event: Event): void {
    event.preventDefault();
    this.goHomeThenScroll('plan', true);
  }

  goToSection(event: Event, sectionId: string): void {
    event.preventDefault();
    this.goHomeThenScroll(sectionId, false);
  }

  private goHomeThenScroll(sectionId: string, focusOrigin: boolean): void {
    const onHome = this.router.url.split('?')[0].split('#')[0] === '/';
    if (!onHome) {
      void this.router.navigate(['/'], { fragment: sectionId }).then(() =>
        this.scrollTo(sectionId, focusOrigin),
      );
      return;
    }
    this.scrollTo(sectionId, focusOrigin);
  }

  private scrollTo(sectionId: string, focusOrigin: boolean): void {
    requestAnimationFrame(() => {
      const el = document.getElementById(sectionId);
      el?.scrollIntoView({ behavior: 'smooth', block: 'start' });
      if (focusOrigin) {
        document.querySelector<HTMLInputElement>('#plan input[name="origin"]')?.focus({
          preventScroll: true,
        });
      }
    });
  }
}
