"""create core tables

Revision ID: 001_create_core_tables
Revises: 
Create Date: 2025-02-14 00:00:00.000000

"""
from alembic import op #type: ignore
import sqlalchemy as sa

revision = '001_create_core_tables'
down_revision = None
branch_labels = None
depends_on = None


def upgrade():
  op.create_table(
    'clientes',
    sa.Column('id', sa.Integer(), primary_key=True),
    sa.Column('created_at', sa.DateTime(), nullable=False),
    sa.Column('updated_at', sa.DateTime(), nullable=False),
    sa.Column('nombre', sa.String(length=120), nullable=False),
    sa.Column('telefono', sa.String(length=40)),
    sa.Column('email', sa.String(length=120)),
    sa.Column('estado', sa.String(length=40)),
  )
  op.create_table(
    'vehiculos',
    sa.Column('id', sa.Integer(), primary_key=True),
    sa.Column('created_at', sa.DateTime(), nullable=False),
    sa.Column('updated_at', sa.DateTime(), nullable=False),
    sa.Column('marca', sa.String(length=80), nullable=False),
    sa.Column('modelo', sa.String(length=80), nullable=False),
    sa.Column('anio', sa.Integer()),
    sa.Column('estado', sa.String(length=40)),
  )
  op.create_table(
    'partes',
    sa.Column('id', sa.Integer(), primary_key=True),
    sa.Column('created_at', sa.DateTime(), nullable=False),
    sa.Column('updated_at', sa.DateTime(), nullable=False),
    sa.Column('nombre', sa.String(length=120), nullable=False),
    sa.Column('stock', sa.Integer(), nullable=False, server_default='0'),
    sa.Column('costo', sa.Numeric(12, 2), nullable=False, server_default='0'),
  )
  op.create_table(
    'reportes',
    sa.Column('id', sa.Integer(), primary_key=True),
    sa.Column('created_at', sa.DateTime(), nullable=False),
    sa.Column('updated_at', sa.DateTime(), nullable=False),
    sa.Column('titulo', sa.String(length=120), nullable=False),
    sa.Column('periodo', sa.String(length=40)),
    sa.Column('generado_el', sa.Date(), nullable=False),
  )
  op.create_table(
    'ventas',
    sa.Column('id', sa.Integer(), primary_key=True),
    sa.Column('created_at', sa.DateTime(), nullable=False),
    sa.Column('updated_at', sa.DateTime(), nullable=False),
    sa.Column('cliente_id', sa.Integer(), nullable=False),
    sa.Column('vehiculo_id', sa.Integer(), nullable=False),
    sa.Column('precio_total', sa.Numeric(12, 2), nullable=False),
    sa.Column('estado', sa.String(length=40)),
    sa.Column('fecha_venta', sa.Date(), nullable=False),
    sa.Column('notas', sa.Text()),
    sa.ForeignKeyConstraint(['cliente_id'], ['clientes.id']),
    sa.ForeignKeyConstraint(['vehiculo_id'], ['vehiculos.id']),
  )
  op.create_table(
    'pagos',
    sa.Column('id', sa.Integer(), primary_key=True),
    sa.Column('created_at', sa.DateTime(), nullable=False),
    sa.Column('updated_at', sa.DateTime(), nullable=False),
    sa.Column('venta_id', sa.Integer(), nullable=False),
    sa.Column('monto', sa.Numeric(12, 2), nullable=False),
    sa.Column('metodo', sa.String(length=40)),
    sa.Column('estado', sa.String(length=40)),
    sa.Column('fecha_pago', sa.Date(), nullable=False),
    sa.ForeignKeyConstraint(['venta_id'], ['ventas.id']),
  )
  op.create_table(
    'ventas_partes',
    sa.Column('id', sa.Integer(), primary_key=True),
    sa.Column('created_at', sa.DateTime(), nullable=False),
    sa.Column('updated_at', sa.DateTime(), nullable=False),
    sa.Column('venta_id', sa.Integer(), nullable=False),
    sa.Column('parte_id', sa.Integer(), nullable=False),
    sa.Column('cantidad', sa.Integer(), nullable=False, server_default='1'),
    sa.Column('precio_unitario', sa.Numeric(12, 2), nullable=False, server_default='0'),
    sa.ForeignKeyConstraint(['parte_id'], ['partes.id']),
    sa.ForeignKeyConstraint(['venta_id'], ['ventas.id']),
  )
  op.create_table(
    'stock_movimientos',
    sa.Column('id', sa.Integer(), primary_key=True),
    sa.Column('created_at', sa.DateTime(), nullable=False),
    sa.Column('updated_at', sa.DateTime(), nullable=False),
    sa.Column('parte_id', sa.Integer(), nullable=False),
    sa.Column('venta_id', sa.Integer()),
    sa.Column('tipo', sa.String(length=20), nullable=False),
    sa.Column('cantidad', sa.Integer(), nullable=False),
    sa.Column('motivo', sa.String(length=120)),
    sa.Column('fecha', sa.Date(), nullable=False),
    sa.ForeignKeyConstraint(['parte_id'], ['partes.id']),
    sa.ForeignKeyConstraint(['venta_id'], ['ventas.id']),
  )


def downgrade():
  op.drop_table('stock_movimientos')
  op.drop_table('ventas_partes')
  op.drop_table('pagos')
  op.drop_table('ventas')
  op.drop_table('reportes')
  op.drop_table('partes')
  op.drop_table('vehiculos')
  op.drop_table('clientes')