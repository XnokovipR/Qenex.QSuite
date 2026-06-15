from System import Single
oiltemp_val = OilTemp.RawValue

with postgres_connection.cursor() as cursor:
	insert_variable_value(cursor, oil_temp_id, oiltemp_val)
 
postgres_connection.commit()

print(f"value changed => {OilTemp.Name}/{oil_temp_id}: {oiltemp_val}")
