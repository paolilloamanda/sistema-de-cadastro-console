using System;
using System.Linq;
using System.Text.RegularExpressions;
using Npgsql;

namespace Cadastro
{
    public class Program
    {
        static string connString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=madmin;";
        private static bool algumCadastroEfetuado;

        public static void Main(string[] args)
        {
            try
            {
                // Horário permitido
                if (VerificarHorarioPermitido())
                {
                    ExecutarSistema();
                }
                else
                {
                    Console.WriteLine("O sistema só pode ser utilizado das 17:00 às 22:00.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ocorreu um erro inesperado: {ex.Message}");
            }
            finally
            {
                PausarSistema();
            }
        }

        private static bool VerificarHorarioPermitido()
        {
            DateTime agora = DateTime.Now;
            TimeSpan inicioPermitido = new TimeSpan(17, 0, 0); // 17:00
            TimeSpan fimPermitido = new TimeSpan(22, 0, 0);    // 22:00
            return (agora.TimeOfDay >= inicioPermitido && agora.TimeOfDay <= fimPermitido);
        }

        public static void ExecutarSistema()
        {
            // Conexão com o banco de dados
            Console.WriteLine("Tentando estabelecer conexão com o banco de dados...");
            try
            {
                if (!TestarConexaoBancoDados())
                {
                    Console.WriteLine("Erro ao conectar ao banco de dados. Verifique as configurações de conexão.");
                    return;
                }

                Console.WriteLine("Cadastro:");

                while (true)
                {
                    CadastrarCliente();

                    Console.WriteLine("Deseja cadastrar outro cliente? (s/n)");
                    string resposta = Console.ReadLine();
                    if (resposta.ToLower() != "s")
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ocorreu um erro ao executar o sistema: {ex.Message}");
            }
        }

        public static void PausarSistema()
        {
            Console.WriteLine("Pressione qualquer tecla para sair...");
            Console.ReadKey();
        }

        public static bool TestarConexaoBancoDados()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();

                    using (var cmd = new NpgsqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = @"
                            CREATE TABLE IF NOT EXISTS clientes (
                                id SERIAL PRIMARY KEY,
                                nome VARCHAR(100) NOT NULL,
                                email VARCHAR(100) NOT NULL,
                                telefone VARCHAR(15) NOT NULL,
                                tipoDocumento VARCHAR(3) NOT NULL,
                                numeroDocumento VARCHAR(20) NOT NULL,
                                dataNascimento DATE NOT NULL,
                                situacao VARCHAR(10) NOT NULL
                            );";
                        cmd.ExecuteNonQuery();
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao conectar ao banco de dados: {ex.Message}");
                return false;
            }
        }

        public static void CadastrarCliente()
        {
            string nome, email, telefone, tipoDocumento, numeroDocumento;
            DateTime dataNascimento;

            Console.Write("Nome: ");
            nome = Console.ReadLine();

            email = LerStringValidada("Email: ", ValidarEmail);

            telefone = LerStringValidada("Telefone: ", ValidarTelefone);

            tipoDocumento = LerStringValidada("Selecione o documento (CPF, RG ou CNH): ", ValidarTipoDocumento);

            numeroDocumento = LerStringValidada("Número do Documento: ", (doc) => ValidarNumeroDocumento(tipoDocumento, doc));

            dataNascimento = LerDataValidada("Data de Nascimento (DD-MM-YYYY): ");

            int idade = CalcularIdade(dataNascimento);

            // Verificar se a idade é válida
            if (idade < 18)
            {
                Console.WriteLine("Cadastro não permitido para menores de 18 anos.");
                return;
            }

            // Exigir RG para maiores de 65 anos
            if (idade >= 65 && tipoDocumento != "RG")
            {
                Console.WriteLine("Para maiores de 65 anos, é necessário fornecer o RG como documento.");
                tipoDocumento = "RG";
                numeroDocumento = LerStringValidada("Número do Documento: ", (doc) => ValidarNumeroDocumento(tipoDocumento, doc));
            }

            // Verificar se o cliente já existe no banco de dados
            if (ClienteJaCadastrado(tipoDocumento, numeroDocumento))
            {
                Console.WriteLine("Cliente já cadastrado no sistema com esse documento.");
                return;
            }

            // Situação cadastral ativa
            string situacaoCadastral = "ativo";

            // Criar objeto Cliente com os dados inseridos
            Cliente cliente = new Cliente
            {
                Nome = nome,
                Email = email,
                Telefone = telefone,
                TipoDocumento = tipoDocumento,
                NumeroDocumento = numeroDocumento,
                DataNascimento = dataNascimento,
                Situacao = situacaoCadastral
            };

            // Inserir cliente no banco de dados
            if (InserirCliente(cliente))
            {
                Console.WriteLine("Cadastro realizado com sucesso.");
                algumCadastroEfetuado = true;
            }
            else
            {
                Console.WriteLine("Erro ao cadastrar o cliente.");
            }
        }

        public static bool InserirCliente(Cliente cliente)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "INSERT INTO clientes (nome, email, telefone, tipoDocumento, numeroDocumento, dataNascimento, situacao) VALUES (@nome, @email, @telefone, @tipoDocumento, @numeroDocumento, @dataNascimento, @situacao)";
                        cmd.Parameters.AddWithValue("@nome", cliente.Nome);
                        cmd.Parameters.AddWithValue("@email", cliente.Email);
                        cmd.Parameters.AddWithValue("@telefone", cliente.Telefone);
                        cmd.Parameters.AddWithValue("@tipoDocumento", cliente.TipoDocumento);
                        cmd.Parameters.AddWithValue("@numeroDocumento", cliente.NumeroDocumento);
                        cmd.Parameters.AddWithValue("@dataNascimento", cliente.DataNascimento);
                        cmd.Parameters.AddWithValue("@situacao", cliente.Situacao);

                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao inserir cliente no banco de dados: {ex.Message}");
                return false;
            }
        }

        public static bool ClienteJaCadastrado(string tipoDocumento, string numeroDocumento)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "SELECT COUNT(*) FROM Clientes WHERE (tipoDocumento = @tipoDocumento AND numeroDocumento = @numeroDocumento) AND situacao = 'ativo'";
                        cmd.Parameters.AddWithValue("@tipoDocumento", tipoDocumento);
                        cmd.Parameters.AddWithValue("@numeroDocumento", numeroDocumento);
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao verificar cliente no banco de dados: {ex.Message}");
                return false;
            }
        }

        public static bool ValidarEmail(string email)
        {
            string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            return Regex.IsMatch(email, pattern);
        }

        public static bool ValidarTelefone(string telefone)
        {
            string pattern = @"^\(\d{2}\)\s\d{4,5}-\d{4}$";
            return Regex.IsMatch(telefone, pattern);
        }

        public static bool ValidarTipoDocumento(string tipoDocumento)
        {
            return tipoDocumento == "CPF" || tipoDocumento == "RG" || tipoDocumento == "CNH";
        }

        public static bool ValidarNumeroDocumento(string tipoDocumento, string numeroDocumento)
        {
            switch (tipoDocumento)
            {
                case "CPF":
                    return ValidarCPF(numeroDocumento);
                case "RG":
                    return ValidarRG(numeroDocumento);
                case "CNH":
                    return ValidarCNH(numeroDocumento);
                default:
                    return false;
            }
        }

        public static bool ValidarCPF(string cpf)
        {
            cpf = new string(cpf.Where(char.IsDigit).ToArray());

            if (cpf.Length != 11)
            {
                return false;
            }

            bool todosDigitosIguais = cpf.Distinct().Count() == 1;
            if (todosDigitosIguais)
            {
                return false;
            }

            int soma = 0;
            for (int i = 0; i < 9; i++)
            {
                soma += int.Parse(cpf[i].ToString()) * (10 - i);
            }
            int resto = soma % 11;
            int primeiroDigitoVerificador = resto < 2 ? 0 : 11 - resto;

            if (int.Parse(cpf[9].ToString()) != primeiroDigitoVerificador)
            {
                return false;
            }

            soma = 0;
            for (int i = 0; i < 10; i++)
            {
                soma += int.Parse(cpf[i].ToString()) * (11 - i);
            }
            resto = soma % 11;
            int segundoDigitoVerificador = resto < 2 ? 0 : 11 - resto;

            if (int.Parse(cpf[10].ToString()) != segundoDigitoVerificador)
            {
                return false;
            }

            // CPF válido
            return true;
        }

        public static bool ValidarRG(string rg)
        {
            if (!Regex.IsMatch(rg, @"^\d{2}\.\d{3}\.\d{3}-\d{1}$"))
            {
                return false;
            }

            string numerosRG = rg.Replace(".", "").Replace("-", "");

            if (!numerosRG.All(char.IsDigit))
            {
                return false;
            }

            if (int.Parse(numerosRG.Substring(0, 8)) == 0)
            {
                return false;
            }

            int[] pesos = { 2, 3, 4, 5, 6, 7, 8, 9 };
            int soma = 0;
            for (int i = 0; i < 8; i++)
            {
                soma += int.Parse(numerosRG[i].ToString()) * pesos[i];
            }
            int resto = soma % 11;
            int digitoVerificador = 11 - resto;
            if (digitoVerificador == 10 || digitoVerificador == 11)
            {
                digitoVerificador = 0;
            }
            int digitoVerificadorEsperado = int.Parse(numerosRG[8].ToString());
            if (digitoVerificador != digitoVerificadorEsperado)
            {
                return false;
            }

            // RG válido
            return true;
        }

        public static bool ValidarCNH(string cnh)
        {
            if (!Regex.IsMatch(cnh, @"^\d{11}$"))
            {
                return false;
            }

            string numerosCNH = cnh;

            if (!numerosCNH.All(char.IsDigit))
            {
                return false;
            }

            int[] pesos = { 9, 8, 7, 6, 5, 4, 3, 2, 9 };
            int soma = 0;
            for (int i = 0; i < 9; i++)
            {
                soma += int.Parse(numerosCNH[i].ToString()) * pesos[i];
            }
            int resto = soma % 11;
            int digitoVerificador = resto >= 10 ? 0 : resto;
            int digitoVerificadorEsperado = int.Parse(numerosCNH[9].ToString());
            if (digitoVerificador != digitoVerificadorEsperado)
            {
                return false;
            }

            soma = 0;
            for (int i = 0; i < 9; i++)
            {
                soma += int.Parse(numerosCNH[i].ToString()) * (pesos[i] - 1);
            }
            soma += digitoVerificador * 2;
            resto = soma % 11;
            digitoVerificador = resto >= 10 ? 0 : resto;
            digitoVerificadorEsperado = int.Parse(numerosCNH[10].ToString());
            if (digitoVerificador != digitoVerificadorEsperado)
            {
                return false;
            }

            // CNH válida
            return true;
        }

        public static DateTime LerDataValidada(string mensagem)
        {
            DateTime dataNascimento;
            bool dataValida = false;

            do
            {
                Console.Write(mensagem);
                string dataNascimentoStr = Console.ReadLine();

                if (DateTime.TryParseExact(dataNascimentoStr, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out dataNascimento))
                {
                    dataValida = true;
                }
                else
                {
                    Console.WriteLine("Data inválida. Digite novamente no formato DD-MM-YYYY:");
                }
            } while (!dataValida);

            return dataNascimento;
        }

        public static string LerStringValidada(string mensagem, Func<string, bool> validacao)
        {
            string input;

            do
            {
                Console.Write(mensagem);
                input = Console.ReadLine();

                if (!validacao(input))
                {
                    Console.WriteLine("Entrada inválida. Tente novamente.");
                }

            } while (!validacao(input));

            return input;
        }

        public static int CalcularIdade(DateTime dataNascimento)
        {
            DateTime hoje = DateTime.Today;
            int idade = hoje.Year - dataNascimento.Year;
            if (dataNascimento.Date > hoje.AddYears(-idade))
            {
                idade--;
            }
            return idade;
        }
    }
}