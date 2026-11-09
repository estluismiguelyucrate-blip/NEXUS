using System.Diagnostics;
using Nexus;

namespace nexus_visual
{
    public partial class Form1 : Form
    {
        private Label lblTitulo = new Label();
        private Button btnIniciar = new Button();
        private Button btnAyuda = new Button();
        private Button btnSalir = new Button();

        public Form1()
        {
            InitializeComponent();
            CrearControles();
        }

        private void CrearControles()
        {
            Text = "NEXUS";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(500, 350);

            lblTitulo = new Label();
            lblTitulo.Text = "NEXUS - Menu principal";
            lblTitulo.AutoSize = true;
            lblTitulo.Font = new Font("Microsoft Sans Serif", 16F, FontStyle.Bold);
            lblTitulo.Location = new Point(125, 45);

            btnIniciar = new Button();
            btnIniciar.Text = "Iniciar juego";
            btnIniciar.Size = new Size(180, 40);
            btnIniciar.Location = new Point(160, 115);
            btnIniciar.Click += BtnIniciar_Click;

            btnAyuda = new Button();
            btnAyuda.Text = "Instrucciones";
            btnAyuda.Size = new Size(180, 40);
            btnAyuda.Location = new Point(160, 170);
            btnAyuda.Click += BtnAyuda_Click;

            btnSalir = new Button();
            btnSalir.Text = "Salir";
            btnSalir.Size = new Size(180, 40);
            btnSalir.Location = new Point(160, 225);
            btnSalir.Click += BtnSalir_Click;

            Controls.Add(lblTitulo);
            Controls.Add(btnIniciar);
            Controls.Add(btnAyuda);
            Controls.Add(btnSalir);
        }

        private void BtnIniciar_Click(object? sender, EventArgs e)
        {
            string ensamblado = typeof(Cadete).Assembly.Location;
            Process.Start(new ProcessStartInfo("dotnet", $"\"{ensamblado}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = false
            });
        }

        private void BtnAyuda_Click(object? sender, EventArgs e)
        {
            MessageBox.Show(
                "Usa las teclas W, A, S y D para moverte.\n" +
                "Completa los objetivos y regresa a la base.",
                "Instrucciones");
        }

        private void BtnSalir_Click(object? sender, EventArgs e)
        {
            Close();
        }
    }
}
