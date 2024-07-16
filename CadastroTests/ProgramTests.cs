using System;
using Xunit;
using Cadastro;

namespace Cadastro.Tests

{
    public class ProgramTests

    {
        [Fact]
        public void TestarConexaoBancoDados_ConexaoValida_DeveRetornarTrue()
        {
            // Arrange & Act 
            bool resultado = Program.TestarConexaoBancoDados();

            // Assert 
            Assert.True(resultado);
        }

        [Fact]
        public void ValidarCPF_CPFValido_DeveRetornarTrue()

        {
            // Arrange 
            string cpf = "529.982.247-25"; // CPF válido 

            // Act 
            bool resultado = Program.ValidarCPF(cpf);

            // Assert 
            Assert.True(resultado);
        }

        [Fact]
        public void ValidarCPF_CPFInvalido_DeveRetornarFalse()

        {
            // Arrange 
            string cpf = "123.456.789-01"; // CPF inválido 

            // Act 
            bool resultado = Program.ValidarCPF(cpf);

            // Assert 
            Assert.False(resultado);
        }

        [Fact]
        public void CalcularIdade_CalculoCorreto_DeveRetornarIdadeCorreta()
        {
            // Arrange 
            DateTime dataNascimento = new DateTime(1990, 6, 20);

            // Act 
            int idadeCalculada = Program.CalcularIdade(dataNascimento);

            // Assert 
            Assert.Equal(34, idadeCalculada);
        }

        [Fact]
        public void InserirCliente_ClienteValido_DeveInserirComSucesso()
        {
            // Arrange 
            Cliente cliente = new Cliente
            {
                Nome = "Fulano",
                Email = "fulano@example.com",
                Telefone = "(11) 99999-9999",
                TipoDocumento = "CPF",
                NumeroDocumento = "529.982.247-25",
                DataNascimento = new DateTime(1980, 5, 19),
                Situacao = "ativo"
            };

            // Act 
            bool resultado = Program.InserirCliente(cliente);

            // Assert 
            Assert.True(resultado);
        }

        [Fact]
        public void ClienteJaCadastrado_ClienteExistente_DeveRetornarTrue()
        {
            // Arrange 
            string tipoDocumento = "CPF";
            string numeroDocumento = "529.982.247-25";

            // Act 
            bool resultado = Program.ClienteJaCadastrado(tipoDocumento, numeroDocumento);

            // Assert 
            Assert.True(resultado);
        }
    }
}