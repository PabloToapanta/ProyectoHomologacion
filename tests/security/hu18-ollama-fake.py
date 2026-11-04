"""Doble HTTP de Ollama exclusivo del smoke aislado. Nunca es un modelo real."""
from http.server import BaseHTTPRequestHandler, HTTPServer
import json


class Handler(BaseHTTPRequestHandler):
    def log_message(self, *args):
        pass

    def do_POST(self):
        if self.headers.get("Transfer-Encoding", "").lower() == "chunked":
            parts = []
            total = 0
            while True:
                size = int(self.rfile.readline().split(b";", 1)[0].strip(), 16)
                if size == 0:
                    self.rfile.readline()
                    break
                total += size
                if total > 65536:
                    self.send_error(413)
                    return
                parts.append(self.rfile.read(size))
                self.rfile.read(2)
            data = b"".join(parts)
        else:
            data = self.rfile.read(int(self.headers.get("Content-Length", "0")))
        # Comprueba también la separación de clientes HTTP y la minimización de datos.
        if self.headers.get("Authorization") or self.headers.get("X-Internal-Key") or b"HU18-SINTETICO" in data:
            self.send_error(400)
            return
        payload = json.loads(data)
        if self.path != "/api/chat" or payload.get("model") != "qwen3:4b" or payload.get("think") is not False:
            self.send_error(400)
            return
        content = json.dumps({"resumen": "Explicación sintética de prueba HTTP, sin inferencia real.", "evidenciasCitadas": ["C1", "R1"]})
        response = json.dumps({"done": True, "done_reason": "stop", "message": {"role": "assistant", "content": content}}).encode()
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(response)))
        self.end_headers()
        self.wfile.write(response)


if __name__ == "__main__":
    HTTPServer(("0.0.0.0", 11434), Handler).serve_forever()
