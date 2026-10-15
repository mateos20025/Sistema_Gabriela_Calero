using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Consultorio.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Citas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bloqueos_agenda",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    motivo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bloqueos_agenda", x => x.id);
                    table.CheckConstraint("ck_bloqueos_agenda_fin_despues_de_inicio", "fin > inicio");
                });

            migrationBuilder.CreateTable(
                name: "citas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    paciente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    origen = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reserva_expira_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    contacto_nombre = table.Column<string>(type: "character varying(201)", maxLength: 201, nullable: true),
                    contacto_telefono = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    contacto_correo = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_citas", x => x.id);
                    table.CheckConstraint("ck_citas_fin_despues_de_inicio", "fin > inicio");
                    table.CheckConstraint("ck_citas_reserva_solo_pendiente", "reserva_expira_en IS NULL OR estado = 'Pendiente'");
                    table.ForeignKey(
                        name: "fk_citas_pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "pacientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "configuracion_agenda",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    duracion_cita_minutos = table.Column<int>(type: "integer", nullable: false),
                    anticipacion_minima_horas = table.Column<int>(type: "integer", nullable: false),
                    anticipacion_maxima_dias = table.Column<int>(type: "integer", nullable: false),
                    horas_reserva_solicitud = table.Column<int>(type: "integer", nullable: false),
                    maximo_solicitudes_activas_por_cedula = table.Column<int>(type: "integer", nullable: false),
                    solicitudes_web_habilitadas = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_configuracion_agenda", x => x.id);
                    table.CheckConstraint("ck_configuracion_agenda_anticipacion", "anticipacion_minima_horas BETWEEN 0 AND 168 AND anticipacion_maxima_dias BETWEEN 1 AND 365");
                    table.CheckConstraint("ck_configuracion_agenda_duracion", "duracion_cita_minutos BETWEEN 10 AND 240");
                });

            migrationBuilder.CreateTable(
                name: "franjas_atencion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dia_semana = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    fin = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_franjas_atencion", x => x.id);
                    table.CheckConstraint("ck_franjas_atencion_fin_despues_de_inicio", "fin > inicio");
                });

            migrationBuilder.CreateTable(
                name: "historial_cita",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cita_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estado_anterior = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    estado_nuevo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    observacion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    inicio_anterior = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    inicio_nuevo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_historial_cita", x => x.id);
                    table.ForeignKey(
                        name: "fk_historial_cita_citas_cita_id",
                        column: x => x.cita_id,
                        principalTable: "citas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_historial_cita_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pagos_cita_id",
                table: "pagos",
                column: "cita_id");

            migrationBuilder.CreateIndex(
                name: "ix_bloqueos_agenda_inicio_fin",
                table: "bloqueos_agenda",
                columns: new[] { "inicio", "fin" });

            migrationBuilder.CreateIndex(
                name: "ix_citas_estado_inicio",
                table: "citas",
                columns: new[] { "estado", "inicio" });

            migrationBuilder.CreateIndex(
                name: "ix_citas_inicio",
                table: "citas",
                column: "inicio");

            migrationBuilder.CreateIndex(
                name: "ix_citas_paciente_id_inicio",
                table: "citas",
                columns: new[] { "paciente_id", "inicio" });

            migrationBuilder.CreateIndex(
                name: "ix_franjas_atencion_dia_semana",
                table: "franjas_atencion",
                column: "dia_semana");

            migrationBuilder.CreateIndex(
                name: "ix_historial_cita_cita_id_creado_en",
                table: "historial_cita",
                columns: new[] { "cita_id", "creado_en" });

            migrationBuilder.CreateIndex(
                name: "ix_historial_cita_usuario_id",
                table: "historial_cita",
                column: "usuario_id");

            migrationBuilder.AddForeignKey(
                name: "fk_pagos_citas_cita_id",
                table: "pagos",
                column: "cita_id",
                principalTable: "citas",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // EF no modela restricciones de exclusión: se crea con SQL. Dos citas activas no pueden
            // cruzarse ([inicio, fin) se solapa con &&). Es la última defensa contra dos personas que
            // toman el mismo horario en el mismo instante; el repositorio traduce el error 23P01.
            migrationBuilder.Sql(
                """
                ALTER TABLE citas ADD CONSTRAINT ex_citas_sin_cruce
                EXCLUDE USING gist (tstzrange(inicio, fin, '[)') WITH &&)
                WHERE (estado IN ('Pendiente', 'EnCola', 'Confirmada', 'Reprogramada', 'NoContesta'));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_pagos_citas_cita_id",
                table: "pagos");

            migrationBuilder.Sql("ALTER TABLE citas DROP CONSTRAINT IF EXISTS ex_citas_sin_cruce;");

            migrationBuilder.DropTable(
                name: "bloqueos_agenda");

            migrationBuilder.DropTable(
                name: "configuracion_agenda");

            migrationBuilder.DropTable(
                name: "franjas_atencion");

            migrationBuilder.DropTable(
                name: "historial_cita");

            migrationBuilder.DropTable(
                name: "citas");

            migrationBuilder.DropIndex(
                name: "ix_pagos_cita_id",
                table: "pagos");
        }
    }
}
