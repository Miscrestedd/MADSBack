using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Threading;

class Program
{
    static System.Timers.Timer timer;
    static double temperaturaCidade = 0;
    static int umidadeCidade = 0;
    static List<double> historicoCO2 = new List<double>();

    static async Task Main(string[] args)
    {
        string portName = "COM4";
        int baudRate = 9600;

        Program program = new Program();
        SerialPort serialPort = new SerialPort(portName, baudRate);
        serialPort.DataReceived += program.SerialPort_DataReceived;

        bool portOpened = false;
        while (!portOpened)
        {
            try
            {
                serialPort.Open();
                portOpened = true;
                Console.WriteLine($"Conectado à porta {portName}. Aguardando dados...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao tentar conectar à porta {portName}: {ex.Message}");
                Console.WriteLine("Tentando novamente em 5 segundos...");
                await Task.Delay(5000);
            }
        }


        try
        {
            //serialPort.Open();
            //Console.WriteLine($"Conectado à porta {portName}. Aguardando dados...");

            await program.GetCityWeatherData();

            System.Timers.Timer timer = new System.Timers.Timer(TimeSpan.FromMinutes(2).TotalMilliseconds);
            timer.Elapsed += (sender, e) => InsertCO2PredictionsCallback(null);
            timer.Start();

            Console.WriteLine("Pressione qualquer tecla para sair...");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro: {ex.Message}");
        }
        finally
        {
            serialPort.Close();
        }
    }

    private static void InsertCO2PredictionsCallback(object state)
    {
        DateTime currentTimestamp = DateTime.Now;

        double predicaoDiaSeguinte = PreverCO2(historicoCO2, 1);
        double predicaoSemanaSeguinte = PreverCO2(historicoCO2, 7);
        double predicaoMesSeguinte = PreverCO2(historicoCO2, 10);

        InsertCO2PredictionsIntoDatabase(currentTimestamp, predicaoDiaSeguinte, predicaoSemanaSeguinte, predicaoMesSeguinte);

        Console.WriteLine($"Previsões de CO2 inseridas na tabela co2_predictions.");

        timer.Start();
    }


    private static async Task InsertCO2PredictionsIntoDatabase(DateTime timestamp, double predictionTomorrow, double predictionIn7Days, double predictionIn30Days)
    {
        string connectionString = "Server=tcp:mads.database.windows.net,1433;Initial Catalog=SQLMADS;Persist Security Info=False;User ID=adminon;Password=Mads123@;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = "INSERT INTO tbPredictions (timestamp, co2_prediction_in_1_day, co2_prediction_in_7_days, co2_prediction_in_30_days) " +
                               "VALUES (@timestamp, @predictionTomorrow, @predictionIn7Days, @predictionIn30Days)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@timestamp", timestamp);
                    command.Parameters.AddWithValue("@predictionTomorrow", predictionTomorrow);
                    command.Parameters.AddWithValue("@predictionIn7Days", predictionIn7Days);
                    command.Parameters.AddWithValue("@predictionIn30Days", predictionIn30Days);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao inserir previsões de CO2 na tabela co2_predictions: {ex.Message}");
        }
    }


    private async Task GetCityWeatherData()
    {
        string apiKey = "e60d95ddf5a2421e991204113242605";
        string cidade = "Sorocaba";

        try
        {
            string url = $"http://api.weatherapi.com/v1/current.json?key={apiKey}&q={cidade}&aqi=no";

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    dynamic data = JsonConvert.DeserializeObject(json);
                    temperaturaCidade = data.current.temp_c;
                    umidadeCidade = data.current.humidity;

                    Console.WriteLine($"Dados de temperatura e umidade da cidade atualizados:");
                    Console.WriteLine($"Temperatura: {temperaturaCidade}°C, Umidade: {umidadeCidade}%");
                }
                else
                {
                    Console.WriteLine($"Erro ao acessar a API de tempo: {response.StatusCode}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao obter dados de temperatura e umidade da cidade: {ex.Message}");
        }
    }

    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        SerialPort serialPort = (SerialPort)sender;
        string data = serialPort.ReadLine();

        const int numeroLeiturasPorPredicao = 10;

        string co2ValueString = data;
        if (double.TryParse(co2ValueString, out double co2Value))
        {
            if (temperaturaCidade != 0 && umidadeCidade != 0)
            {
                double k = 0.0005;

                double co2Ajustado = k * co2Value * temperaturaCidade + umidadeCidade;

                Console.WriteLine($"Sensor data: {data}");
                Console.WriteLine($"Process data (weather): {co2Ajustado}");

                historicoCO2.Add(co2Ajustado);

                if (historicoCO2.Count >= numeroLeiturasPorPredicao)
                {
                    double predicaoDiaSeguinte = PreverCO2(historicoCO2, 1);
                    double predicaoSemanaSeguinte = PreverCO2(historicoCO2, 7);
                    double predicaoMesSeguinte = PreverCO2(historicoCO2, 30);

                    string cityLOC = "SOROCABA";

                    InsertSensorDataIntoDatabase(DateTime.Now, co2Value, co2Ajustado, temperaturaCidade, umidadeCidade, cityLOC);

                    Console.WriteLine($"Dados inseridos na tabela sensor_data.");

                    Console.WriteLine($"Predições: Dia seguinte: {predicaoDiaSeguinte}, Semana seguinte: {predicaoSemanaSeguinte}, Mês seguinte: {predicaoMesSeguinte}");
                }
                else
                {
                    Console.WriteLine("Ainda não há dados suficientes para fazer uma predição.");
                }
            }
            else
            {
                Console.WriteLine("Dados de temperatura e umidade da cidade não estão disponíveis.");
            }
        }
        else
        {
            Console.WriteLine($"Erro ao converter valor de CO2: {co2ValueString}");
        }
    }

    private static void InsertSensorDataIntoDatabase(DateTime timestamp, double rawSensorValue, double processedSensorValue, double apiTemperature, double apiHumidity, string city)
    {
        string connectionString = "Server=tcp:mads.database.windows.net,1433;Initial Catalog=SQLMADS;Persist Security Info=False;User ID=adminon;Password=Mads123@;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "INSERT INTO tbSensorData (timestamp, raw_sensor_value, processed_sensor_value, api_temperature, api_humidity, city) " +
                               "VALUES (@timestamp, @rawSensorValue / 100.0, @processedSensorValue, @apiTemperature, @apiHumidity, @city)";


                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@timestamp", timestamp);
                    command.Parameters.AddWithValue("@rawSensorValue", rawSensorValue);
                    command.Parameters.AddWithValue("@processedSensorValue", processedSensorValue);
                    command.Parameters.AddWithValue("@apiTemperature", apiTemperature);
                    command.Parameters.AddWithValue("@apiHumidity", apiHumidity);
                    command.Parameters.AddWithValue("@city", city);

                    command.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao inserir dados na tabela sensor_data: {ex.Message}");
        }
    }

    private static double PreverCO2(List<double> historicoCO2, int dias)
    {
        int startIndex = historicoCO2.Count - dias;
        if (startIndex < 0) startIndex = 0;

        if (historicoCO2.Count >= dias)
        {
            List<double> valoresParaPredicao = historicoCO2.GetRange(startIndex, dias);

            double media = valoresParaPredicao.Average();

            return media;
        }
        else
        {
            Console.WriteLine("Não há dados suficientes para fazer uma predição.");
            return 0;
        }
    }
}
