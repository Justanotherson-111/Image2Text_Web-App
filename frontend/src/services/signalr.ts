import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("/api/hubs/ocr")
  .withAutomaticReconnect()
  .build();

export async function startSignalR() {
  if (connection.state === signalR.HubConnectionState.Disconnected) {
    await connection.start();
    console.log("SignalR connected");
  }
}

export function onOcrProgress(
  callback: (imageId: string, progress: number) => void
) {
  connection.on("OcrProgress", (data) => {
    callback(data.imageId, data.progress);
  });
}

export function onOcrCompleted(
  callback: (imageId: string) => void
) {
  connection.on("OcrCompleted", (data) => {
    callback(data.imageId);
  });
}

export async function joinImageRoom(imageId: string) {
  if (connection.state !== signalR.HubConnectionState.Connected) {
    await startSignalR();
  }
  await connection.invoke("JoinImageRoom", imageId);
}

export async function leaveImageRoom(imageId: string) {
  if (connection.state === signalR.HubConnectionState.Connected) {
    await connection.invoke("LeaveImageRoom", imageId);
  }
}
