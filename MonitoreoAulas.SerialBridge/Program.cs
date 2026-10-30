
using System.IO.Ports;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

const string nombrePuerto = "COM6";
const int velocidad = 9600;

const string urlApi = "https://localhost:44376/";

using var httpClient = new HttpClient
{
    BaseAddress = new Uri(urlApi),
    Timeout = TimeSpan.FromSeconds(10)
};

using var puertoSerial = new SerialPort(
    nombrePuerto,
    velocidad,
    Parity.None,
    8,
    StopBits.One)
{
    Encoding = Encoding.UTF8,
    NewLine = "\n",
    ReadTimeout = 1000,
    Handshake = Handshake.None
};

using var cancelacion = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancelacion.Cancel();
};

string idSesion = Guid.NewGuid().ToString("D");
long secuencia = 0;

Console.Title = "MonitoreoAulas - SerialBridge";

Console.WriteLine("************************************");
Console.WriteLine(" MONITOREO DE OCUPACION DE AULAS");
Console.WriteLine(" SERIAL BRIDGE - .NET 10");
Console.WriteLine("************************************");

Console.WriteLine($"[SERIAL] Puerto: {nombrePuerto}");
Console.WriteLine($"[API] Destino: {urlApi}");
Console.WriteLine();

try
{
    puertoSerial.Open();

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("[SERIAL] Puerto conectado");
    Console.ResetColor();

    while (!cancelacion.IsCancellationRequested)
    {
        string linea;

        try
        {
            linea = puertoSerial.ReadLine().Trim();
        }
        catch (TimeoutException)
        {
            continue;
        }

        if (string.IsNullOrWhiteSpace(linea))
            continue;

        if (!linea.StartsWith("{"))
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"[ARDUINO] {linea}");
            Console.ResetColor();

            continue;
        }

        JsonElement datos;

        try
        {
            using var documento = JsonDocument.Parse(linea);
            datos = documento.RootElement.Clone();

            _ = datos.GetProperty("aula").GetString();
            _ = datos.GetProperty("evento").GetString();
            _ = datos.GetProperty("ocupacion").GetInt32();
            _ = datos.GetProperty("capacidad").GetInt32();
            _ = datos.GetProperty("tiempoMs").GetInt64();
        }
        catch (Exception ex) when (
            ex is JsonException ||
            ex is KeyNotFoundException ||
            ex is InvalidOperationException)
        {
            Console.WriteLine($"[JSON] Mensaje invalido: {ex.Message}");

            continue;
        }

        string evento = datos.GetProperty("evento")
            .GetString()!
            .ToUpperInvariant();

        if (evento == "INICIO")
        {
            idSesion = Guid.NewGuid().ToString("D");
            secuencia = 0;

            Console.WriteLine($"[SISTEMA] Nueva sesion: {idSesion}");
        }

        secuencia++;

        var solicitud = new EventoApiRequest(
            datos.GetProperty("aula").GetString()!,
            evento,
            datos.GetProperty("ocupacion").GetInt32(),
            datos.GetProperty("capacidad").GetInt32(),
            datos.GetProperty("tiempoMs").GetInt64(),
            idSesion,
            secuencia
        );

        Console.WriteLine(
            $"[EVENTO] {evento} | " +
            $"Ocupacion: {solicitud.Ocupacion}");

        await EnviarConReintentosAsync(
            httpClient,
            solicitud,
            cancelacion.Token);
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("[SISTEMA] Cancelado");
}
catch (Exception ex) when (
    ex is IOException ||
    ex is UnauthorizedAccessException ||
    ex is InvalidOperationException)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"[ERROR SERIAL] {ex.Message}");
    Console.ResetColor();
}
finally
{
    if (puertoSerial.IsOpen)
        puertoSerial.Close();

    Console.WriteLine("[SISTEMA] Finalizado");
}

static async Task EnviarConReintentosAsync(
    HttpClient cliente,
    EventoApiRequest evento,
    CancellationToken cancellationToken)
{
    const int maxIntentos = 3;

    for (int intento = 1;
         intento <= maxIntentos;
         intento++)
    {
        try
        {
            using var respuesta = await cliente.PostAsJsonAsync(
                "api/ocupacion/eventos",
                evento,
                cancellationToken);

            if (respuesta.IsSuccessStatusCode)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(
                    $"[API] Evento confirmado: {evento.Evento} " +
                    $"| HTTP {(int)respuesta.StatusCode}");
                Console.ResetColor();

                return;
            }

            if ((int)respuesta.StatusCode >= 400 &&
                (int)respuesta.StatusCode < 500)
            {
                string detalle =
                    await respuesta.Content.ReadAsStringAsync(cancellationToken);

                Console.WriteLine($"[API] Evento rechazado: {detalle}");

                return;
            }

            Console.WriteLine(
                $"[API] Error HTTP {(int)respuesta.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine(
                $"[API] Error de conexion: {ex.Message}");
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("[API] Tiempo de espera agotado");
        }

        if (intento < maxIntentos)
        {
            Console.WriteLine($"[API] Reintento {intento + 1}/{maxIntentos}");

            await Task.Delay(2000, cancellationToken);
        }
    }

    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(
        $"[API] ENVIO FALLIDO. Sesion: {evento.IdSesion}, " +
        $"Secuencia: {evento.Secuencia}");
    Console.ResetColor();
}

public sealed record EventoApiRequest(
    string Aula,
    string Evento,
    int Ocupacion,
    int Capacidad,
    long TiempoMs,
    string IdSesion,
    long Secuencia
);
