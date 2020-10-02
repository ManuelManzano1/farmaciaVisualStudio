using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.IO;
using iTextSharp.text.pdf;
using iTextSharp.text;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;

namespace tpvFarmacia
{
    public partial class formGestion : Form
    {
        conectarBD cnx;
        List<claseMedicamento> listaMedicamento = new List<claseMedicamento>();
        String nombreImagen;
        claseMedicamento med = new claseMedicamento();
        String pdf;
        public formGestion()
        {
            InitializeComponent();
        }

        private void formGestion_Load(object sender, EventArgs e)
        {
            cnx = new conectarBD();
            listaMedicamento = cnx.listar();
            dataGridView1.DataSource = listaMedicamento;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                FileStream fs = new FileStream(nombreImagen, FileMode.Open, FileAccess.Read);
                BinaryReader br = new BinaryReader(fs);
                byte[] bloque = br.ReadBytes((int)fs.Length);
                cnx.Insertar(txtNombre.Text, Convert.ToDouble(txtPrecio.Text), bloque, Convert.ToInt16(txtStockMin.Text), Convert.ToInt16(txtStockActual.Text));
            }
            catch (Exception)
            {
                MessageBox.Show("datos incompletos");
            }

         }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            OpenFileDialog op1 = new OpenFileDialog();
            op1.Filter = "imagenes|*.jpg;*.png";
            if (op1.ShowDialog()==DialogResult.OK)
            {
                nombreImagen = op1.FileName;
                pictureBox1.Image = System.Drawing.Image.FromFile(nombreImagen);
            }
           
        }

        private void txt_DoubleClick(object sender, EventArgs e)
        {
        
        }

        private void dataGridView1_RowHeaderMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            
        }

        private void dataGridView1_RowHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            txtNombreMod.Text = listaMedicamento[dataGridView1.CurrentRow.Index].Nombre;
            txtPrecioMod.Text = Convert.ToString(listaMedicamento[dataGridView1.CurrentRow.Index].Precio);
            txtStockActMod.Text = Convert.ToString(listaMedicamento[dataGridView1.CurrentRow.Index].Stockactual);
            txtStockMinMod.Text = Convert.ToString(listaMedicamento[dataGridView1.CurrentRow.Index].Stockminimo);
            MemoryStream ms = new MemoryStream(listaMedicamento[dataGridView1.CurrentRow.Index].Imagen);
            pictureBoxMod.Image = System.Drawing.Image.FromStream(ms);
            med.Imagen = listaMedicamento[dataGridView1.CurrentRow.Index].Imagen;
            lbIndice.Text = Convert.ToString(listaMedicamento[dataGridView1.CurrentRow.Index].Indice);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            med.Indice = Convert.ToInt16(lbIndice.Text);
            med.Nombre = txtNombreMod.Text;
            med.Precio = Convert.ToDouble(txtPrecioMod.Text);
            med.Stockminimo = Convert.ToInt16(txtStockMinMod.Text);
            med.Stockactual = Convert.ToInt16(txtStockActMod.Text);
            cnx.modificarMedicamento(med);
            listaMedicamento.Clear();
            listaMedicamento = cnx.listar();
            dataGridView1.DataSource = null;
            dataGridView1.DataSource = listaMedicamento;
        }

        private void pictureBoxMod_Click(object sender, EventArgs e)
        {
            String imagen;
            try
            {
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    imagen = openFileDialog1.FileName;
                    pictureBoxMod.Image = System.Drawing.Image.FromFile(imagen);
                    FileStream fs = new FileStream(imagen, FileMode.Open, FileAccess.Read);
                    long tamanio = fs.Length;

                    BinaryReader br = new BinaryReader(fs);
                    byte[] bloque = br.ReadBytes((int)fs.Length);
                    fs.Read(bloque, 0, Convert.ToInt32(tamanio));

                    med.Imagen = bloque;
                    // MemoryStream ms = new MemoryStream(bloque);

                    //  listadoMedicamento[dataGridView1.CurrentRow.Index].Imagen = bloque;


                    // cargarBotones();
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show("El archivo seleccionado no es un tipo de imagen");
            }
        }

        private void formGestion_Leave(object sender, EventArgs e)
        {
            
        }

        private void button4_Click(object sender, EventArgs e)
        {
            pdf = crearPdf();
            mandarMail(pdf);
           
        }

        private void mandarMail(string pdf)
        {
            try
            {
                string email = "manuvimanzano@gmail.com";
                string password = txtpwd.Text;

                var loginInfo = new NetworkCredential(email, password);
                var msg = new MailMessage();
                var smtpClient = new SmtpClient("smtp.gmail.com", 25);

                msg.From = new MailAddress(email);
                msg.To.Add(new MailAddress(txtmail.Text));
                msg.Subject = "Pedido medicamentos";
                msg.Body = "Lista medicamentos que se necesita reponer";
                System.Net.Mail.Attachment attachment;
                attachment = new System.Net.Mail.Attachment(pdf);
                msg.Attachments.Add(attachment);



                msg.IsBodyHtml = true;

                smtpClient.EnableSsl = true;
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = loginInfo;

                smtpClient.Send(msg);
                smtpClient.Dispose();
            }
            catch (Exception ex)
            {

            }
        }

        private string crearPdf()
        {
            List<claseMedicamento> lista = new List<claseMedicamento>();
            lista = cnx.medicamentosStockBajo();
            PdfPTable pdfTable = new PdfPTable(lista.Count);
            pdfTable.DefaultCell.Padding = 3;
            pdfTable.WidthPercentage = 80;
            pdfTable.HorizontalAlignment = Element.ALIGN_CENTER;
            pdfTable.DefaultCell.BorderWidth = 1;

            pdfTable.AddCell("Nombre");
            pdfTable.AddCell("Cantidad necesaria");
            //Recorrrer el arrayList donde estan los medicamentos que tienen el stockactual
            //Por debajo del stock minimo
            for (int i = 0; i < lista.Count; i++)
            {
                pdfTable.AddCell(lista[i].Nombre);
                pdfTable.AddCell(Convert.ToString(lista[i].Stockminimo - lista[i].Stockactual));
            }
            pdfTable.AddCell(DateTime.Now.ToString("MM-dd-yy"));

            string folderPath = "C:\\ticket\\";

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            string nombreTicket = DateTime.Now.ToString("MM-dd-yy_HH-mm-ss") + ".pdf";
            folderPath += nombreTicket;
            using (FileStream stream = new FileStream(folderPath, FileMode.Create))
            {
                Document pdfDoc = new Document(PageSize.A6, 10f, 10f, 10f, 0f);
                PdfWriter.GetInstance(pdfDoc, stream);
                pdfDoc.Open();
                pdfDoc.Add(pdfTable);

                pdfDoc.Close();
                stream.Close();
            }
            Process pc = new Process();
            pc.StartInfo.FileName = folderPath;
            pc.Start();
            return folderPath;
        }

        private void txtMedicamento_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter){
                cnx.aniadirMedicamento(Convert.ToInt16(txtMedicamento.Text));
                MessageBox.Show("Producto añadido correctamente");
                txtMedicamento.Text = "";
            }

            }

        private void button5_Click(object sender, EventArgs e)
        {
            cnx.ExportarBd();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            String nombreFichero = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                 nombreFichero = openFileDialog1.FileName;
            }
            cnx.importarBD(nombreFichero);
            MessageBox.Show("Datos importados correctamente");
        }

    }
}
