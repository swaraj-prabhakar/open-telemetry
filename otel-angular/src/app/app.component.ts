import { HttpClient } from '@angular/common/http';
import { Component } from '@angular/core';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  title = 'otel-angular';

  constructor(private http: HttpClient) {}

  getWeatherForecast() {
    this.http.get('http://localhost:9901/weather-forecast/from-other-service').subscribe();
  }
}
