# Sistema de Monitoramento de Qualidade do Ar e Previsão de CO2

<img src="https://www.tomsonelectronics.com/cdn/shop/products/mq-135_2048x2048.png?v=1555048157" alt="Sensor de CO2" width="70%">

Este projeto consiste em um sistema de monitoramento ambiental que combina dados de um sensor de CO2 com informações meteorológicas para calcular e prever níveis de dióxido de carbono (CO2) em uma determinada localidade. O sistema utiliza uma conexão serial para obter dados do sensor e acessa uma API de tempo para obter informações atualizadas de temperatura e umidade.

## Funcionalidades

- **Monitoramento Contínuo**: Captura e processa dados do sensor de CO2 em tempo real.
- **Previsão de CO2**: Utiliza histórico de dados para prever os níveis de CO2 futuros.
- **Integração com API de Tempo**: Obtém e utiliza dados de temperatura e umidade de uma API externa.
- **Armazenamento em Banco de Dados**: Registra leituras do sensor e previsões em um banco de dados SQL.

## Tecnologias Utilizadas

- C#
- .NET Core
- SerialPort (COM4)
- HttpClient para integração com API de tempo
- Newtonsoft.Json para manipulação de dados JSON
- SQL Server para armazenamento de dados

---

## Contribuição

Contribuições são bem-vindas! Para modificações importantes, abra uma issue para discutir o que você gostaria de mudar.

---

## Licença

Este projeto está licenciado sob a [MIT License](https://opensource.org/licenses/MIT).
