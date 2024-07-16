using System;

namespace Cadastro
{
    public class Cliente
    {
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Telefone { get; set; }
        public string TipoDocumento { get; set; }
        public string NumeroDocumento { get; set; }
        public DateTime DataNascimento { get; set; }
        public string Situacao { get; set; }
    }
}