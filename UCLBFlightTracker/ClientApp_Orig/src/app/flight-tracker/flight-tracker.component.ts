import { Component, OnInit } from '@angular/core';
import * as signalR from '@aspnet/signalr';

export interface SimUpdate {
  latitude: number;
  longitude: number;
  altitude: number;
  groundTrack: number;
  groundSpeed: number;
  parkingBrake: boolean;
  onGround: boolean;
  trueHeading: number;
  tailNumber: string;
  airline: string;
  flightNumber: string;
  model: string;
  stallWarning: boolean;
  overspeedWarning: boolean;
  fuelQty: number;
  gpsapproachTimeDeviation: boolean;
}

@Component({
  selector: 'app-flight-tracker',
  templateUrl: './flight-tracker.component.html',
  styleUrls: ['./flight-tracker.component.scss']
})
export class FlightTrackerComponent implements OnInit {

  private connection: signalR.HubConnection;

  connected: boolean;

  constructor() { }

  ngOnInit() {
    this.startConnection();
  }

  startConnection() {
    this.connection = new signalR.HubConnectionBuilder().withUrl('/simhub').build();
    this.connection
      .start()
      .then(() => {
        console.log("Connection started");
        this.connected = true;
        this.listenForUpdates();
      })
      .catch(err => console.error("Error while starting connection " + err));
  }

  listenForUpdates() {
    this.connection.on('PositionObject', (pos) => {
      console.log(pos);
    });
  }

  requestInit() {
    this.connection.invoke('initPlugin').catch(err => console.error(err));
  }
}
